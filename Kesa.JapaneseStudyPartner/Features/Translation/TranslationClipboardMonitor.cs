using Avalonia.Controls;
using Avalonia.VisualTree;
using Kesa.Japanese.Features;
using System;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Kesa.JapaneseStudyPartner.Features.Translation;

public static class TranslationClipboardMonitor
{
    public static event EventHandler ClipboardChanged;

    public static void Initialize()
    {
        if (Design.IsDesignMode || AppEnvironment.MainWindow.GetVisualRoot() is not TopLevel window)
        {
            return;
        }

        var handle = new HWND(window.TryGetPlatformHandle()!.Handle);
        _ = PInvoke.AddClipboardFormatListener(handle);
        _ = PInvoke.SetWindowSubclass(handle, OnSubclassCallback, nuint.Zero, nuint.Zero);
    }

    private static LRESULT OnSubclassCallback(HWND hwnd, uint code, WPARAM wParam, LPARAM lParam, nuint ignored1, nuint ignored2)
    {
        if (code == 0x31D)
        {
            ClipboardChanged?.Invoke(null, EventArgs.Empty);
        }

        return PInvoke.DefSubclassProc(hwnd, code, wParam, lParam);
    }
}