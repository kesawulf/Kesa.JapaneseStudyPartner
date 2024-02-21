﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Dictionary;
using Kesa.Japanese.Features.Sentences;
using Kesa.Japanese.Features.Settings;
using Kesa.Japanese.Features.Translation;

namespace Kesa.Japanese.Features.Main;

public partial class MainWindowViewModel : ViewModelBase
{
    public TranslationViewModel TranslationViewModel { get; private set; }

    public DictionaryViewModel DictionaryViewModel { get; private set; }

    public SentencesViewModel SentencesViewModel { get; private set; }

    public SettingsViewModel SettingsViewModel { get; private set; }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public void Initialize()
    {
        TranslationViewModel = new TranslationViewModel();
        DictionaryViewModel = new DictionaryViewModel();
        SentencesViewModel = new SentencesViewModel();
        SettingsViewModel = new SettingsViewModel();

        OnPropertyChanged(nameof(TranslationViewModel));
        OnPropertyChanged(nameof(DictionaryViewModel));
        OnPropertyChanged(nameof(SentencesViewModel));
        OnPropertyChanged(nameof(SettingsViewModel));

        CurrentPage = TranslationViewModel;
    }

    [RelayCommand]
    public void SetPage(object value) => CurrentPage = value as ViewModelBase;
}