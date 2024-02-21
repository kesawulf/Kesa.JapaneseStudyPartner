using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kesa.Japanese.Common;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _currentGoogleCredentialsFilePath;

    [ObservableProperty]
    private string _currentWaniKaniApiKey;

    [ObservableProperty]
    private string _currentDeepLApiKey;

    [ObservableProperty]
    private string _currentSaplingApiKey;

    public SettingsViewModel()
    {
        OnResetChangesPressed();
    }

    [RelayCommand]
    public async Task PickGoogleCredentialsFilePath()
    {
        // Start async operation to open the dialog.
        var files = await AppEnvironment.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose Credentials File",
            AllowMultiple = false,
        });

        if (files is [{ } file])
        {
            CurrentGoogleCredentialsFilePath = file.Path.LocalPath;
        }
    }

    [RelayCommand]
    public void OnSaveApplyPressed()
    {
        var settings = AppEnvironment.Settings;
        settings.GoogleCredentialsFilePath = CurrentGoogleCredentialsFilePath;
        settings.WaniKaniApiKey = CurrentWaniKaniApiKey;
        settings.DeepLApiKey = CurrentDeepLApiKey;
        settings.SaplingApiKey = CurrentSaplingApiKey;
        settings.Save();

        AppEnvironment.MainWindowViewModel.TranslationViewModel.ReloadServices(false);
    }

    [RelayCommand]
    public void OnResetChangesPressed()
    {
        var settings = AppEnvironment.Settings;
        CurrentGoogleCredentialsFilePath = settings.GoogleCredentialsFilePath;
        CurrentWaniKaniApiKey = settings.WaniKaniApiKey;
        CurrentDeepLApiKey = settings.DeepLApiKey;
        CurrentSaplingApiKey = settings.SaplingApiKey;
    }
}