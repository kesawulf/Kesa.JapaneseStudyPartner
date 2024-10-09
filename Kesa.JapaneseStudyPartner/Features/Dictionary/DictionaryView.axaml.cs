using Avalonia.Controls;
using Kesa.Japanese.Features.Sentences;
using System;
using System.Reactive.Linq;

namespace Kesa.Japanese.Features.Dictionary;

public partial class DictionaryView : UserControl
{
    public DictionaryView()
    {
        InitializeComponent();

        IDisposable current = null;

        DataContextChanged += (sender, args) =>
        {
            current?.Dispose();
            current = null;

            if (DataContext is not DictionaryViewModel vm)
            {
                return;
            }

            var observable = Observable.FromEvent(
                handler => vm.PageLoaded += handler,
                handler => vm.PageLoaded -= handler);

            current = new ScrollViewerAutoLoader<DictionaryViewModel>(ScrollViewer, vm, observable, async viewModel =>
            {
                if (viewModel != null && viewModel.DictionaryItems.Count > 0)
                {
                    await viewModel.LoadNextPageAsync();
                }
            });
        };
    }
}