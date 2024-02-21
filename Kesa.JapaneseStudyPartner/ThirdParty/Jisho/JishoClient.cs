using HtmlAgilityPack;
using Kesa.Japanese.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Kesa.Japanese.ThirdParty.Jisho;

public class JishoClient
{
    private HttpClient Client { get; } = new();

    public JishoSentenceResponse[] SearchSentences(string query)
    {
        var items = new List<JishoSentenceResponse>();

        try
        {
            var url = $"https://jisho.org/search/{Uri.EscapeDataString(query)}%23sentences";
            var doc = Client.GetHtmlAsync(url).GetAwaiter().GetResult();

            foreach (var node in doc.DocumentNode.SelectNodes("//div[contains(@class, 'sentence_content')]")?.ToArray() ?? [])
            {
                var japaneseNode = node.SelectSingleNode("ul[contains(@class, 'japanese_sentence')]");
                var japaneseText = new StringBuilder();

                foreach (var japaneseChildNode in japaneseNode.ChildNodes)
                {
                    if (japaneseChildNode.NodeType == HtmlNodeType.Text)
                    {
                        japaneseText.Append(japaneseChildNode.InnerText);
                    }
                    else
                    {
                        var nodeWithText = japaneseChildNode.SelectSingleNode("span[contains(@class, 'unlinked')]");
                        japaneseText.Append(nodeWithText.InnerText);
                    }
                }

                var englishNode = node.SelectSingleNode("div/span[contains(@class, 'english')]");
                items.Add(new JishoSentenceResponse()
                {
                    JapaneseText = japaneseText.ToString().Trim(),
                    PronunciationText = japaneseText.ToString().Trim().GetJapanesePronunciation(),
                    EnglishText = englishNode.InnerText.Trim()
                });
            }
        }
        catch
        {
            //Do nothing
        }

        return items.ToArray();
    }

    public IEnumerable<JishoDefinitionResponse> SearchDefinitions(string query) => new JishoDefinitionResponseEnumerator(Client, query);
}

public class JishoDefinitionResponseEnumerator(HttpClient client, string query) : IEnumerable<JishoDefinitionResponse>
{
    public IEnumerable<JishoDefinitionResponse> GetResults(int page)
    {
        var items = new List<JishoDefinitionResponse>();

        try
        {
            var url = $"https://jisho.org/search/{HttpUtility.UrlPathEncode(query)}?page={page}";
            var doc = client.GetHtmlAsync(url).GetAwaiter().GetResult();

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'concept_light-meanings')]")
                ?.Select(node => node.ParentNode)
                .ToArray() ?? [];

            foreach (var node in nodes)
            {
                var meanings = node.SelectNodes(".//span[@class='meaning-meaning']")
                    .Select(static node => node.InnerText.Trim())
                    .ToArray();

                if (node.SelectSingleNode(".//div[@class='concept_light-representation']//span[@class='furigana']") is not { } furiganaNode)
                {
                    //Filter out names
                    continue;
                }

                var furigana = furiganaNode.ChildNodes
                    .Select(static node => node.Name == "ruby" ? node.SelectSingleNode(".//rt") : node)
                    .Where(static node => node.Name != "ruby" && node.Name != "rb")
                    .SkipWhile(static node => node.NodeType == HtmlNodeType.Text && node.InnerText.Trim().Length == 0)
                    ?.ToArray() ?? [];

                if (furigana.Length == 0)
                {
                    continue;
                }

                var text = node.SelectSingleNode(".//span[@class='text']")
                    .ChildNodes
                    .Where(static node => node.NodeType != HtmlNodeType.Text || node.InnerText.Trim().Length > 0)
                    .ToArray();

                var (japaneseText, pronunciationText) = GeneratePronunciation(text, furigana);

                items.Add(new JishoDefinitionResponse()
                {
                    Text = japaneseText,
                    Meaning = HttpUtility.HtmlDecode(string.Join(Environment.NewLine, meanings)),
                    Pronunciation = pronunciationText
                });
            }
        }
        catch
        {
            //Do nothing
        }

        return items.ToArray();
    }

    private static (string Text, string Pronunciation) GeneratePronunciation(HtmlNode[] textNodes, HtmlNode[] furiganaNodes)
    {
        var text = new StringBuilder();
        var pronunciation = new StringBuilder();

        using var furiEnumerator = furiganaNodes.Cast<HtmlNode>().GetEnumerator();

        foreach (var node in textNodes)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                foreach (var character in node.InnerText.Trim())
                {
                    furiEnumerator.MoveNext();
                    text.Append(character);
                    pronunciation.Append(furiEnumerator.Current.InnerText.Trim());
                }
            }
            else
            {
                furiEnumerator.MoveNext();
                text.Append(node.InnerText.Trim());
                pronunciation.Append(node.InnerText.Trim());
            }
        }

        return (text.ToString(), pronunciation.ToString());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<JishoDefinitionResponse> GetEnumerator()
    {
        int page = 1;
        int count = 0;

        while (true)
        {
            foreach (var result in GetResults(page))
            {
                yield return result;
                count++;
            }

            yield break;

            //if (count > 0)
            //{
            //    page++;
            //    count = 0;
            //}
            //else
            //{
            //    yield break;
            //}
        }
    }
}

public class JishoDefinitionResponse
{
    public string Text { get; set; }

    public string Pronunciation { get; set; }

    public string Meaning { get; set; }
}

public class JishoSentenceResponse
{
    public string JapaneseText { get; set; }

    public string PronunciationText { get; set; }

    public string EnglishText { get; set; }
}