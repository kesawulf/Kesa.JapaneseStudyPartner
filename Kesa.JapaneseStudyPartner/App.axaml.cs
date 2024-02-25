using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Kesa.Japanese.Features;
using Kesa.Japanese.Features.Main;
using Kesa.JapaneseStudyPartner.Features.Translation;

namespace Kesa.Japanese;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            AppEnvironment.Initialize((MainWindow)desktop.MainWindow);
            AppEnvironment.MainWindowViewModel.Initialize();
        }

        base.OnFrameworkInitializationCompleted();

        TranslationClipboardMonitor.Initialize();
    }
}