using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Dictionary;
using Kesa.Japanese.Features.Segmentation;
using Kesa.Japanese.Features.Sentences;
using Kesa.Japanese.Features.Settings;
using Kesa.Japanese.Features.Translation;
using Kesa.Japanese.Features.WaniKaniExport;

namespace Kesa.Japanese.Features.Main;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    public TranslationViewModel _translationViewModel;

    [ObservableProperty]
    public DictionaryViewModel _dictionaryViewModel;

    [ObservableProperty]
    public SentencesViewModel _sentencesViewModel;

    [ObservableProperty]
    public SegmentationViewModel _segmentationViewModel;

    [ObservableProperty]
    public WaniKaniExportViewModel _waniKaniExportViewModel;

    [ObservableProperty]
    public SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public void Initialize()
    {
        TranslationViewModel = new TranslationViewModel();
        DictionaryViewModel = new DictionaryViewModel();
        SentencesViewModel = new SentencesViewModel();
        SettingsViewModel = new SettingsViewModel();
        SegmentationViewModel = new SegmentationViewModel();
        WaniKaniExportViewModel = new WaniKaniExportViewModel();

        CurrentPage = TranslationViewModel;
    }

    [RelayCommand]
    public void SetPage(object value) => CurrentPage = value as ViewModelBase;
}