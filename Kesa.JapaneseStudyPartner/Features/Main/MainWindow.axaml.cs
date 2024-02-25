using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using System.Linq;

namespace Kesa.Japanese.Features.Main;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        Navigation.SelectedItem = Navigation.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => item.Tag == viewModel.TranslationViewModel);
    }

    private void Navigation_OnItemInvoked(object sender, NavigationViewItemInvokedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (args.IsSettingsInvoked)
        {
            viewModel.SetPage(viewModel.SettingsViewModel);
        }
        else
        {
            viewModel.SetPage(args.InvokedItemContainer.Tag);
        }
    }
}