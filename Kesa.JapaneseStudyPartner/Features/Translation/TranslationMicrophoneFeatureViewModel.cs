using Avalonia.Svg.Skia;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Kesa.Japanese.Common;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Translation;

public enum TranslationMicrophoneFeatureMicrophoneState
{
    None,
    NotRecording,
    Changing,
    Recording,
}

public enum TranslationMicrophoneFeatureMessages
{
    MicrophoneInputReceived,
}

public partial class TranslationMicrophoneFeatureViewModel : ViewModelBase
{
    private readonly object _micLock = new();
    private readonly Queue<ByteString> _micQueue = [];
    private readonly WaveInEvent _micSource;
    private SpeechClient.StreamingRecognizeStream _activeCall;
    private TranslationMicrophoneFeatureMicrophoneState _translationMicrophoneFeatureMicrophoneState;
    private int _micVersion;

    public TranslationMicrophoneFeatureMicrophoneState MicrophoneState
    {
        get => _translationMicrophoneFeatureMicrophoneState;
        set
        {
            if (SetProperty(ref _translationMicrophoneFeatureMicrophoneState, value))
            {
                OnPropertyChanged(nameof(MicrophoneImage));
            }
        }
    }

    public SvgImage MicrophoneImage
    {
        get
        {
            var image = new SvgImage();

            var path = MicrophoneState switch
            {
                TranslationMicrophoneFeatureMicrophoneState.None or TranslationMicrophoneFeatureMicrophoneState.NotRecording => "Assets.Icons.Mic.svg",
                TranslationMicrophoneFeatureMicrophoneState.Changing => "Assets.Icons.MicChanging.svg",
                TranslationMicrophoneFeatureMicrophoneState.Recording => "Assets.Icons.MicActive.svg",
                _ => throw new ArgumentOutOfRangeException()
            };
            var svg = SvgSource.LoadFromStream(Utilities.GetEmbeddedResourceStream(path));
            image.Source = svg;
            return image;
        }
    }

    public TranslationMicrophoneFeatureViewModel()
    {
        _micSource = new WaveInEvent();
        _micSource.WaveFormat = new WaveFormat(16000, 1);
        _micSource.DataAvailable += async (_, args) =>
        {
            lock (_micLock)
            {
                _micQueue.Enqueue(ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded));
            }

            await Task.Yield();
        };

        MicrophoneState = TranslationMicrophoneFeatureMicrophoneState.NotRecording;
    }

    [RelayCommand]
    public void ToggleRecording()
    {
        if (MicrophoneState == TranslationMicrophoneFeatureMicrophoneState.Recording)
        {
            Task.Factory.StartNew(StopRecordingAsync);
        }
        else if (MicrophoneState == TranslationMicrophoneFeatureMicrophoneState.NotRecording)
        {
            Task.Factory.StartNew(StartRecordingAsync);
        }
    }

    public async Task StopRecordingAsync()
    {
        if (MicrophoneState != TranslationMicrophoneFeatureMicrophoneState.Recording)
        {
            return;
        }

        MicrophoneState = TranslationMicrophoneFeatureMicrophoneState.Changing;

        lock (_micLock)
        {
            _micSource.StopRecording();
            _micQueue.Clear();
            _micVersion++;
        }

        await _activeCall.WriteCompleteAsync();
        _activeCall = null;
        MicrophoneState = TranslationMicrophoneFeatureMicrophoneState.NotRecording;
    }

    public async Task StartRecordingAsync()
    {
        if (MicrophoneState != TranslationMicrophoneFeatureMicrophoneState.NotRecording)
        {
            return;
        }

        MicrophoneState = TranslationMicrophoneFeatureMicrophoneState.Changing;
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
        MicrophoneState = TranslationMicrophoneFeatureMicrophoneState.Recording;

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
        var lastGuess = "";

        while (await stream.MoveNextAsync())
        {
            foreach (var result in stream.Current.Results)
            {
                if (result.IsFinal || result.Stability > 0.35)
                {
                    lastGuess = result.Alternatives.First().Transcript;
                }

                lastGuess = lastGuess.Replace(" ", "。");
                AppEnvironment.SendMessage(TranslationMicrophoneFeatureMessages.MicrophoneInputReceived, lastGuess);
            }
        }

        AppEnvironment.SendMessage(TranslationMicrophoneFeatureMessages.MicrophoneInputReceived, lastGuess);
    }
}