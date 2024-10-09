using Avalonia.Media;
using Kesa.Japanese.Common;
using Kesa.Japanese.ThirdParty.Jisho;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Sentences;

public partial class SentencesViewModel : ViewModelBase
{
    private string _searchText;
    private int _currentPage;
    private bool _currentPageLast;

    public ObservableCollection<SentenceItemViewModel> SentenceItems { get; } = [];

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
                SentenceItems.Clear();

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
        SentenceItems.Clear();
        await LoadNextPageAsync();
    }

    public async Task LoadNextPageAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || _currentPageLast)
        {
            return;
        }

        var results = await AppEnvironment.JishoClient.SearchSentencesAsync(SearchText, ++_currentPage);
        if (results is not { Length: > 0 })
        {
            _currentPageLast = true;
            return;
        }

        foreach (var result in results)
        {
            SentenceItems.Add(new SentenceItemViewModel(result, SearchText));
        }

        PageLoaded?.Invoke();
    }
}

public class JapaneseTextDivisionViewModel
{
    public string JapaneseText { get; set; }

    public string FuriganaText { get; set; }

    public IBrush Foreground { get; set; }
}

public partial class SentenceItemViewModel(JishoSentenceResponse sentenceResponse, string searchTerm) : ViewModelBase
{
    public string EnglishText => sentenceResponse.EnglishText;

    public JapaneseTextDivisionViewModel[] SentenceDivisions => Utilities.DivideAndColorJapaneseText(sentenceResponse.JapaneseText, searchTerm);
}