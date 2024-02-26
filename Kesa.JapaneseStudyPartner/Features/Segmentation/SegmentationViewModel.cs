using Kesa.Japanese.Common;
using Kesa.Japanese.ThirdParty.IchiMoe;
using System.Collections.ObjectModel;
using System.Text;
using System.Web;

namespace Kesa.Japanese.Features.Segmentation;

public partial class SegmentationViewModel : ViewModelBase
{
    private string _searchText;

    public ObservableCollection<SegmentationItemViewModel> SegmentationItems { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                const string SearchKey = $"{nameof(SegmentationViewModel)}.{nameof(SearchText)}";

                AppEnvironment.Debounce.Cancel(SearchKey);
                SegmentationItems.Clear();

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
        SegmentationItems.Clear();

        foreach (var result in AppEnvironment.IchiMoeClient.Query(SearchText))
        {
            SegmentationItems.Add(new SegmentationItemViewModel(result));
        }
    }
}

public partial class SegmentationItemViewModel(IchiMoeGloss gloss) : ViewModelBase
{
    public string Text => GenerateText();

    public string GenerateText()
    {
        var result = new StringBuilder();

        result.AppendLine(gloss.Word);

        foreach (var alt in gloss.Alternatives)
        {
            result.Append($"  {alt}");
        }

        var num = 0;
        foreach (var conj in gloss.Conjugations)
        {
            result.AppendLine();
            result.AppendLine($"{conj.VerbType} {conj.ConjugationType}");

            num = 0;
            foreach (var def in conj.Definitions)
            {
                result.AppendLine($"  {++num}. {def.PartOfSpeech} {def.Description}");
            }
        }

        result.AppendLine();

        num = 0;
        foreach (var def in gloss.Definitions)
        {
            result.AppendLine($"{++num}. {def.PartOfSpeech} {def.Description}");
        }

        return HttpUtility.HtmlDecode(result.ToString().Trim());
    }
}

