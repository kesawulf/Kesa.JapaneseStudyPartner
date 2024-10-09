using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Kesa.Japanese.Features.Sentences;

public partial class SentencesView : UserControl
{
    public SentencesView()
    {
        InitializeComponent();

        IDisposable current = null;

        DataContextChanged += (sender, args) =>
        {
            current?.Dispose();
            current = null;

            if (DataContext is not SentencesViewModel vm)
            {
                return;
            }

            var observable = Observable.FromEvent(
                handler => vm.PageLoaded += handler,
                handler => vm.PageLoaded -= handler);

            current = new ScrollViewerAutoLoader<SentencesViewModel>(ScrollViewer, vm, observable, async viewModel =>
            {
                if (viewModel != null && viewModel.SentenceItems.Count > 0)
                {
                    await viewModel.LoadNextPageAsync();
                }
            });
        };
    }
}

public class ScrollViewerAutoLoader<TContext> : IDisposable
{
    private CompositeDisposable _disposable;
    private double _verticalHeightMax;

    public ScrollViewerAutoLoader(ScrollViewer scrollViewer, TContext context, IObservable<Unit> loadedObservable, Action<TContext> loadMoreItems)
    {
        _disposable = new();

        var maximumSubscription = scrollViewer
            .GetObservable(ScrollViewer.ScrollBarMaximumProperty)
            .Skip(1)
            .Subscribe(newMax =>
            {
                _verticalHeightMax = newMax.Y;

                if (newMax.Y == 0)
                {
                    loadMoreItems(context);
                }
            });

        var offsetSubscription = scrollViewer
            .GetObservable(ScrollViewer.OffsetProperty)
            .Skip(1)
            .Subscribe(offset =>
            {
                if (Math.Abs(_verticalHeightMax - offset.Y) <= double.Epsilon)
                {
                    loadMoreItems(context);
                }
            });

        var loadedSubscription = loadedObservable?.Skip(1).Subscribe(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_verticalHeightMax == 0)
                {
                    loadMoreItems(context);
                }
            }, DispatcherPriority.Background);
        });

        _disposable.Add(maximumSubscription);
        _disposable.Add(offsetSubscription);
        _disposable.Add(loadedSubscription);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}