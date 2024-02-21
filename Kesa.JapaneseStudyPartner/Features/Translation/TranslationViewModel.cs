using Avalonia.Svg.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Speech.V1;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using Kesa.Japanese.Common;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Translation;

public partial class TranslationViewModel : ViewModelBase
{
    private readonly object _micLock = new();
    private readonly Queue<ByteString> _micQueue = [];
    private SpeechClient.StreamingRecognizeStream _activeCall;
    private WaveInEvent _micSource;
    private MicStates _micState;
    private int _micVersion;

    [ObservableProperty]
    private string _pronunciationText;

    [ObservableProperty]
    private string _outputText;

    [ObservableProperty]
    private bool _loadingTranslationService;

    [ObservableProperty]
    private string _lookupText;

    [ObservableProperty]
    private string _selectionInfo;

    private string _inputText;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                PronunciationText = "";
                OutputText = "";

                const string TranslateInputTextKey = nameof(TranslateInputTextKey);

                if (string.IsNullOrEmpty(value))
                {
                    AppEnvironment.Debounce.Cancel(TranslateInputTextKey);
                    return;
                }

                AppEnvironment.Debounce.Execute(TranslateInputTextKey, 333, TranslateInputText);
            }
        }
    }

    public enum MicStates
    {
        None,
        NotRecording,
        Changing,
        Recording,
    }

    public MicStates MicState
    {
        get => _micState;
        set
        {
            if (SetProperty(ref _micState, value))
            {
                OnPropertyChanged(nameof(MicImage));
            }
        }
    }

    public SvgImage MicImage
    {
        get
        {
            var image = new SvgImage();
            var svg = new SvgSource();

            var path = _micState switch
            {
                MicStates.None or MicStates.NotRecording => "Assets.Icons.Mic.svg",
                MicStates.Changing => "Assets.Icons.MicChanging.svg",
                MicStates.Recording => "Assets.Icons.MicActive.svg",
                _ => throw new ArgumentOutOfRangeException()
            };

            svg.Load(Utilities.GetEmbeddedResourceStream(path));
            image.Source = svg;
            return image;
        }
    }

    public TranslationViewModel()
    {
        ClipboardWatcher.ClipboardChanged += async (s, e) => await OnClipboardChanged();
        ReloadServices(true);
    }

    public void ReloadServices(bool isInitialLoad)
    {
        LoadingTranslationService = true;

        _ = Task.Factory.StartNew(async () =>
        {
            if (isInitialLoad)
            {
                ConfigureIpaDict();

                _micSource = new WaveInEvent();
                _micSource.WaveFormat = new WaveFormat(16000, 1);
                _micSource.DataAvailable += async (s, e) =>
                {
                    lock (_micLock)
                    {
                        _micQueue.Enqueue(ByteString.CopyFrom(e.Buffer, 0, e.BytesRecorded));
                    }

                    await Task.Yield();
                };
            }
            else
            {
                await StopRecordingAsync();
            }

            MicState = MicStates.NotRecording;
            LoadingTranslationService = false;
        });
    }

    [RelayCommand]
    public void ToggleRecording()
    {
        if (MicState == MicStates.Recording)
        {
            Task.Factory.StartNew(StopRecordingAsync);
        }
        else if (MicState == MicStates.NotRecording)
        {
            Task.Factory.StartNew(StartRecordingAsync);
        }
    }

    private async Task StopRecordingAsync()
    {
        if (MicState != MicStates.Recording)
        {
            return;
        }

        MicState = MicStates.Changing;

        lock (_micLock)
        {
            _micSource.StopRecording();
            _micQueue.Clear();
            _micVersion++;
        }

        await _activeCall.WriteCompleteAsync();
        _activeCall = null;
        MicState = MicStates.NotRecording;
    }

    private async Task StartRecordingAsync()
    {
        if (MicState != MicStates.NotRecording)
        {
            return;
        }

        MicState = MicStates.Changing;
        _activeCall = AppEnvironment.SpeechClient.StreamingRecognize();

        await _activeCall.WriteAsync(new StreamingRecognizeRequest()
        {
            StreamingConfig = new StreamingRecognitionConfig()
            {
                Config = new RecognitionConfig()
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "ja-JP",
                    Model = "default",
                },
                InterimResults = true,
            }
        });

        var version = _micVersion;
        _ = Task.Factory.StartNew(HandleReceivedSpeechToText);

        _micSource.StartRecording();
        MicState = MicStates.Recording;

        while (_micVersion == version)
        {
            ByteString data;

            lock (_micLock)
            {
                _micQueue.TryDequeue(out data);
            }

            if (data != null)
            {
                try
                {
                    await _activeCall?.WriteAsync(new StreamingRecognizeRequest() { AudioContent = data });
                }
                catch
                {
                    lock (_micLock)
                    {
                        _micQueue.Clear();
                    }
                }
            }
        }
    }

    private async Task HandleReceivedSpeechToText()
    {
        var stream = _activeCall.GetResponseStream();
        var fullText = "";

        while (await stream.MoveNextAsync())
        {
            foreach (var result in stream.Current.Results)
            {
                var text = result.Alternatives.First().Transcript;
                string currentGuess;

                if (result.IsFinal)
                {
                    fullText += text;
                    currentGuess = "";
                }
                else
                {
                    currentGuess = text;
                }

                InputText = fullText + currentGuess;
            }
        }

        InputText = fullText;
    }

    public static void ConfigureIpaDict()
    {
        Directory.CreateDirectory("IpaDic");
        CopyResource("char.bin");
        CopyResource("sys.dic");
        CopyResource("unk.dic");
        CopyResource("matrix.bin");

        static void CopyResource(string file)
        {
            if (!File.Exists("IpaDic/" + file))
            {
                File.WriteAllBytes("IpaDic/" + file, Utilities.GetEmbeddedResourceStream(file).ReadToByteArray());
            }
        }
    }

    public void TranslateInputText(KeyedDebounceActionContext context)
    {
        var text = InputText;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        OutputText = "Translating...";

        var isInputJapanese = text.HasJapaneseText();
        var response = isInputJapanese
            ? AppEnvironment.DeepLClient.Translate(text, "ja", "en-US")
            : AppEnvironment.DeepLClient.Translate(text, "en", "ja");

        var textToPronounce = isInputJapanese
            ? text
            : response;

        if (!context.IsActive)
        {
            return;
        }

        if (textToPronounce.Any(c => c.IsKanji()))
        {
            PronunciationText = textToPronounce.GetJapanesePronunciation();
        }

        OutputText = response;
    }

    private async Task OnClipboardChanged()
    {
        if (await AppEnvironment.MainWindow.Clipboard.GetDataAsync("PNG") is byte[] clipboardImageData)
        {
            using var ms = new MemoryStream(clipboardImageData);

            var request = new AnnotateImageRequest();
            request.Image = Image.FromStream(ms);
            request.Features.Add(new Feature() { Type = Feature.Types.Type.TextDetection });

            var response = AppEnvironment.AnnotationClient.Annotate(request);
            if (response.FullTextAnnotation is { } result)
            {
                InputText = result.Text;
            }
        }
    }
}