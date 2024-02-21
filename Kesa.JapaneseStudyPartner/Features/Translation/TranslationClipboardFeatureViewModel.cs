using Google.Cloud.Vision.V1;
using Kesa.Japanese.Common;
using System;
using System.Diagnostics;
using System.IO;

namespace Kesa.Japanese.Features.Translation;

public enum TranslationClipboardFeatureMessages
{
    ClipboardImageTextReceived,
}

public class TranslationClipboardFeatureViewModel : ViewModelBase
{
    private readonly Stopwatch _lastClipboardEditTime = Stopwatch.StartNew();
    private byte[] _lastClipboardImageContents;

    public TranslationClipboardFeatureViewModel()
    {
        ClipboardWatcher.ClipboardChanged += OnClipboardChanged;
    }

    protected override void HandleIsWindowFocusedChanged(bool value)
    {
        if (value && _lastClipboardEditTime.IsRunning && _lastClipboardEditTime.Elapsed.TotalSeconds < 5 && _lastClipboardImageContents != null)
        {
            ProcessClipboardImageContents();
        }
    }

    private async void OnClipboardChanged(object sender, EventArgs eventArgs)
    {
        _lastClipboardEditTime.Stop();

        if (await AppEnvironment.MainWindow.Clipboard!.GetDataAsync("PNG") is byte[] clipboardImageData)
        {
            _lastClipboardImageContents = clipboardImageData;

            if (IsWindowFocused)
            {
                ProcessClipboardImageContents();
            }
            else
            {
                _lastClipboardEditTime.Restart();
            }
        }
    }

    private void ProcessClipboardImageContents()
    {
        if (_lastClipboardImageContents is { } contents)
        {
            _lastClipboardImageContents = null;

            using var ms = new MemoryStream(contents);

            var request = new AnnotateImageRequest();
            request.Image = Image.FromStream(ms);
            request.Features.Add(new Feature() { Type = Feature.Types.Type.TextDetection });

            var response = AppEnvironment.AnnotationClient.Annotate(request);
            if (response.FullTextAnnotation is { } result)
            {
                AppEnvironment.SendMessage(TranslationClipboardFeatureMessages.ClipboardImageTextReceived, result.Text);
            }
        }
    }
}