using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Sentences;
using Kesa.Japanese.ThirdParty.Jisho;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Dictionary;

public partial class DictionaryViewModel : ViewModelBase
{
    private string _searchText;
    private int _currentPage;
    private bool _currentPageLast;

    public ObservableCollection<DictionaryItemViewModel> DictionaryItems { get; } = [];

    public event Action PageLoaded;

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

                AppEnvironment.Debounce.Execute(SearchKey, 333, async _ => await UpdateSearchResultsAsync());
            }
        }
    }

    private async Task UpdateSearchResultsAsync()
    {
        _currentPage = 0;
        _currentPageLast = false;
        DictionaryItems.Clear();

        await LoadNextPageAsync();
    }

    public async Task LoadNextPageAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || _currentPageLast)
        {
            return;
        }

        var results = await AppEnvironment.JishoClient.SearchDefinitionsAsync(SearchText, ++_currentPage);
        if (results is not { Length: > 0 })
        {
            _currentPageLast = true;
            return;
        }

        foreach (var result in results)
        {
            DictionaryItems.Add(new DictionaryItemViewModel(result));
        }

        PageLoaded?.Invoke();
    }
}

public partial class DictionaryItemViewModel(JishoDefinitionResponse definitionResponse) : ViewModelBase
{
    public string Text => $"{definitionResponse.Text} ({definitionResponse.Pronunciation}){Environment.NewLine}{Environment.NewLine}{definitionResponse.Meaning}";
}