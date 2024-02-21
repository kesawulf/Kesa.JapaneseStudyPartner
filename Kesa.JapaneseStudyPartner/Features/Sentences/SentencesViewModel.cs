using Kesa.Japanese.Common;
using Kesa.Japanese.ThirdParty.Jisho;
using System;
using System.Collections.ObjectModel;

namespace Kesa.Japanese.Features.Sentences;

public partial class SentencesViewModel : ViewModelBase
{
    private string _searchText;

    public ObservableCollection<SentenceItemViewModel> SentenceItems { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                const string SearchKey = $"{nameof(SentencesViewModel)}.{nameof(SearchText)}";

                AppEnvironment.Debounce.Cancel(SearchKey);
                SentenceItems.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                AppEnvironment.Debounce.Execute(SearchKey, 333, _ => UpdateSearchResults());
            }
        }
    }

    private void UpdateSearchResults()
    {
        SentenceItems.Clear();

        foreach (var result in AppEnvironment.JishoClient.SearchSentences(SearchText))
        {
            SentenceItems.Add(new SentenceItemViewModel(result));
        }
    }
}

public partial class SentenceItemViewModel(JishoSentenceResponse sentenceResponse) : ViewModelBase
{
    public string Text => $"{sentenceResponse.JapaneseText}{Environment.NewLine}{sentenceResponse.PronunciationText}{Environment.NewLine}{sentenceResponse.EnglishText}";
}