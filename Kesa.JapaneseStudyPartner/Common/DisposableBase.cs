using System;
using System.Threading;

namespace Kesa.Japanese.Common;

public abstract class DisposableBase : IDisposable
{
    private int _disposedValue;
    private CancellationTokenSource _cancellationTokenSource;

    public CancellationToken DisposeCancellationToken => _cancellationTokenSource.Token;

    public bool IsDisposed => _disposedValue == 1;

    protected DisposableBase()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedValue, 1, 0) == 0)
        {
            GC.SuppressFinalize(this);
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            OnDispose(true);
        }
    }

    public void Dispose(bool disposeManagedObjects)
    {
        if (Interlocked.CompareExchange(ref _disposedValue, 1, 0) == 0)
        {
            GC.SuppressFinalize(this);

            if (disposeManagedObjects)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            OnDispose(disposeManagedObjects);
        }
    }

    protected abstract void OnDispose(bool disposeManagedObjects);
}