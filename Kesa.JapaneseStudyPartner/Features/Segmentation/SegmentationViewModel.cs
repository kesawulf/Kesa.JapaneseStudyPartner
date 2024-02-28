using Kesa.Japanese.Common;
using Kesa.Japanese.ThirdParty.IchiMoe;
using System.Collections.ObjectModel;
using System.Text;
using System.Web;

namespace Kesa.Japanese.Features.Segmentation;

public partial class SegmentationViewModel : ViewModelBase
{
    private string _sentenceText;

    public ObservableCollection<SegmentationItemViewModel> SegmentationItems { get; } = [];

    public string SentenceText
    {
        get => _sentenceText;
        set
        {
            if (SetProperty(ref _sentenceText, value))
            {
                const string SearchKey = $"{nameof(SegmentationViewModel)}.{nameof(SentenceText)}";

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

        try
        {
            foreach (var result in AppEnvironment.IchiMoeClient.Query(SentenceText))
            {
                SegmentationItems.Add(new SegmentationItemViewModel(result));
            }
        }
        catch
        {
            //Do nothing
        }
    }
}

public partial class SegmentationItemViewModel(IchiMoeEntry entry) : ViewModelBase
{
    public string Text => GenerateText();

    public string GenerateText()
    {
        var result = new StringBuilder();
        WriteEntry(result, 0, entry);
        return HttpUtility.HtmlDecode(result.ToString().Trim());
    }

    private void WriteEntry(StringBuilder result, int indentation, IchiMoeEntry entry)
    {
        AppendLineIndented($"{entry.Text}");

        if (entry.CompoundDescription != null)
        {
            AppendLineIndentedBy(1, $"{entry.CompoundDescription}");
            AppendLine();
        }

        if (entry.SuffixDescription != null)
        {
            AppendLineIndentedBy(1, entry.SuffixDescription);
            AppendLine();
        }

        foreach (var compound in entry.Compounds)
        {
            WriteEntry(result, indentation + 1, compound);
        }

        var num = 0;

        foreach (var conj in entry.Conjugations)
        {
            AppendLineIndentedBy(1, conj.IsNegative
                ? $"{conj.VerbType} {conj.ConjugationType} (Negative)"
                : $"{conj.VerbType} {conj.ConjugationType}");

            foreach (var def in conj.Words)
            {
                WriteEntry(result, indentation + 1, def);
            }

            AppendLine();
        }

        num = 0;
        foreach (var def in entry.Definitions)
        {
            AppendLineIndentedBy(1, $"{++num}. {def.PartOfSpeech} {def.Description}");
        }


        void AppendLine() => result.AppendLine();

        void AppendLineIndented(string text) => result.AppendLine($"{Indent(indentation)}{text}");

        void AppendLineIndentedBy(int additional, string text) => result.AppendLine($"{Indent(indentation + additional)}{text}");
    }

    private string Indent(int amount) => "".PadLeft(amount * 2, ' ');
}

