using CommunityToolkit.Mvvm.ComponentModel;
using Kesa.Japanese.Features;

namespace Kesa.Japanese.Common;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isWindowFocused;

    protected ViewModelBase()
    {
        if (AppEnvironment.IsInitialized)
        {
            HandleInitialization();
        }
        else
        {
            AppEnvironment.Initialized += HandleInitialization;
        }

    }

    private void HandleInitialization()
    {
        AppEnvironment.MainWindow.Activated += (s, e) => IsWindowFocused = true;
        AppEnvironment.MainWindow.Deactivated += (s, e) => IsWindowFocused = false;
    }

    partial void OnIsWindowFocusedChanged(bool value) => HandleIsWindowFocusedChanged(value);

    protected virtual void HandleIsWindowFocusedChanged(bool value) { }
}