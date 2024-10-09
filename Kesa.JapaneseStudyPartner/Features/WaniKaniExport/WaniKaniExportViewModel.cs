using CommunityToolkit.Mvvm.Input;
using Kesa.Japanese.Common;
using Kesa.Japanese.ThirdParty.Anki;
using Kesa.WaniKaniApi;
using Kesa.WaniKaniApi.Endpoints.Assignments;
using Kesa.WaniKaniApi.Endpoints.Subjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.WaniKaniExport;

using WaniKaniSubjectList = List<WaniKaniResponse<WaniKaniSubject>>;

/// <summary>
/// Lol this literally only exists for me don't look at this yet
/// </summary>
public partial class WaniKaniExportViewModel : ViewModelBase
{
    [RelayCommand]
    public async void Export()
    {
        await ImportWaniKaniBurnedCardsToAnki(AppEnvironment.WaniKaniClient, AppEnvironment.AnkiConnectClient);
    }

    public static async Task ImportWaniKaniBurnedCardsToAnki(WaniKaniClient waniKaniClient, AnkiConnectClient ankiClient)
    {
        var assignments = await waniKaniClient.Assignments.GetAsync(new WaniKaniAssignmentEndpointGetOptions() { Burned = true, SubjectTypes = ["kana_vocabulary", "vocabulary"], }).ToListAsync();
        var assignmentIds = new HashSet<int>(assignments.Select(a => a.Data.SubjectId));
        var subjects = await waniKaniClient.Subjects.GetAsync().ToArrayAsync();

        var subjectsById = subjects.ToDictionary(s => s.Id);

        var cardIds = await ankiClient.MakeRequestAsync<long[]>("findCards", "query", "deck:*");
        var cardInfos = await ankiClient.MakeRequestAsync<AnkiConnectCardInfo[]>("cardsInfo", "cards", cardIds.Result);
        var cardValuesToDeck = cardInfos.Result
            .SelectMany(info => info.Fields.Select(f => f.Value.Value), (cardInfo, field) => (Card: cardInfo, Deck: cardInfo.DeckName, Field: field))
            .ToDictionaryByGroup(tuple => tuple.Field, tuple => (tuple.Card, tuple.Deck));

        var userInfo = await waniKaniClient.UserInfo.GetAsync();

        var inNoDeckItems = new WaniKaniSubjectList();

        foreach (var subject in assignments.Select(assignment => subjectsById[assignment.Data.SubjectId]))
        {
            if (subject.ObjectType == WaniKaniResponseObjectType.Radical)
            {
                continue;
            }

            if (true
                && !cardValuesToDeck.TryGetValue(subject.Data.Slug, out _)
                && !cardValuesToDeck.TryGetValue(subject.Data.PrimaryMeaning, out _)
                && (subject.ObjectType != WaniKaniResponseObjectType.KanaVocabulary && !cardValuesToDeck.TryGetValue($"{subject.Data.Slug}[{subject.Data.PrimaryReading}]", out _))
                && (subject.ObjectType != WaniKaniResponseObjectType.KanaVocabulary && !cardValuesToDeck.TryGetValue(getRubyText(subject), out _))
                && !cardValuesToDeck.TryGetValue(await Utilities.ToRubyTextAsync(subject.Data.Slug), out _))
            {
                inNoDeckItems.Add(subject);
            }
        }

        var burnedCardsNotYetInDeck = inNoDeckItems.ToDictionaryByGroup(subject => subject.Data.Level);
        var addCardRequests = new List<AnkiConnectAddNoteRequest>();

        foreach (var (level, burnedCard) in burnedCardsNotYetInDeck.Flatten())
        {
            if (level > userInfo.Level)
            {
                continue;
            }

            var text = burnedCard.Data.Slug;
            var reading = burnedCard.Data.PrimaryReading;

            var deck = $"Japanese::WaniKani Burned Cards::Level {level.ToString("D2")}";
            var addCardRequest = new AnkiConnectAddNoteRequest()
            {
                DeckName = deck,
                ModelName = "Japanese Vocabulary",
                Fields = new Dictionary<string, string>()
                {
                    { "Japanese", getRubyText(burnedCard) },
                    { "Reading", reading },
                    { "Meaning", burnedCard.Data.PrimaryMeaning },
                },
            };
            addCardRequests.Add(addCardRequest);

            Trace.WriteLine($"{text} -> {getRubyText(burnedCard)}");
        }

        var createdNoteIds = await ankiClient.MakeRequestAsync<object[]>("addNotes", "notes", addCardRequests);

        string getRubyText(WaniKaniResponse<WaniKaniSubject> subject)
        {
            var text = subject.Data.Slug;

            if (subject.ObjectType == WaniKaniResponseObjectType.KanaVocabulary)
            {
                return text;
            }

            var reading = subject.Data.PrimaryReading;
            var sb = new StringBuilder();

            if (text[0].IsKanji())
            {
                for (int offset = 1; offset <= Math.Min(text.Length, reading.Length); offset++)
                {
                    var readCharacter = reading[^offset];
                    var slugCharacter = text[^offset];

                    if (readCharacter == slugCharacter || (readCharacter.IsHiragana() && slugCharacter.IsKatakana()))
                    {
                        sb.Insert(0, slugCharacter);
                    }
                    else
                    {
                        offset--;
                        var front = $" {text[..^offset]}[{reading[..^offset]}]";
                        sb.Insert(0, front);
                        break;
                    }
                }
            }
            else if (text[^1].IsKanji())
            {
                for (int index = 0; index < Math.Min(text.Length, reading.Length); index++)
                {
                    var readCharacter = reading[index];
                    var slugCharacter = text[index];

                    if (readCharacter == slugCharacter || (readCharacter.IsHiragana() && slugCharacter.IsKatakana()))
                    {
                        sb.Append(slugCharacter);
                    }
                    else
                    {
                        var front = $" {text[index..]}[{reading[index..]}]";
                        sb.Append(front);
                        break;
                    }
                }
            }

            if (sb.Length >= 1 && sb[0] == ' ')
            {
                sb.Remove(0, 1);
            }

            if (sb.Length >= 1 && (sb[0] == '~' || sb[0] == '～' || sb[0] == '〜'))
            {
                sb.Insert(1, ' ');
            }

            return sb.ToString().Trim();
        }
    }
}
