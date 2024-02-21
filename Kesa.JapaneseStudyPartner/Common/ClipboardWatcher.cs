using Avalonia.Controls;
using Avalonia.VisualTree;
using Kesa.Japanese.Features;
using System;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Kesa.Japanese.Common;

public static class ClipboardWatcher
{
    public static event EventHandler ClipboardChanged;

    public static void Initialize()
    {
        if (AppEnvironment.MainWindow.GetVisualRoot() is not TopLevel topLevel ||
            topLevel.TryGetPlatformHandle()?.Handle is not { } windowHandle)
        {
            return;
        }

        var handle = new HWND(windowHandle);
        PInvoke.AddClipboardFormatListener(handle);
        PInvoke.SetWindowSubclass(handle, OnSubclassCallback, UIntPtr.Zero, UIntPtr.Zero);
    }

    private static LRESULT OnSubclassCallback(HWND hwnd, uint code, WPARAM wParam, LPARAM lParam, UIntPtr ignored1, UIntPtr ignored2)
    {
        if (code == 0x31D)
        {
            ClipboardChanged?.Invoke(null, EventArgs.Empty);
        }

        return PInvoke.DefSubclassProc(hwnd, code, wParam, lParam);
    }
}