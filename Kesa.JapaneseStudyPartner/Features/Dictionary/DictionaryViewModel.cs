using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Sentences;
using Kesa.Japanese.ThirdParty.Jisho;
using System;
using System.Collections.ObjectModel;

namespace Kesa.Japanese.Features.Dictionary;

public partial class DictionaryViewModel : ViewModelBase
{
    private string _searchText;

    public ObservableCollection<DictionaryItemViewModel> DictionaryItems { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                const string SearchKey = $"{nameof(SentencesViewModel)}.{nameof(SearchText)}";

                AppEnvironment.Debounce.Cancel(SearchKey);
                DictionaryItems.Clear();

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
        DictionaryItems.Clear();

        foreach (var result in AppEnvironment.JishoClient.SearchDefinitions(SearchText))
        {
            DictionaryItems.Add(new DictionaryItemViewModel(result));
        }
    }
}

public partial class DictionaryItemViewModel(JishoDefinitionResponse definitionResponse) : ViewModelBase
{
    public string Text => $"{definitionResponse.Text} ({definitionResponse.Pronunciation}){Environment.NewLine}{Environment.NewLine}{definitionResponse.Meaning}";
}