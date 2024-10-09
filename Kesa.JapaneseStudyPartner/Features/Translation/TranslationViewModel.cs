using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Sentences;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Kesa.Japanese.Features.Translation;

public partial class TranslationViewModel : ViewModelBase
{

    [ObservableProperty]
    private bool _loadingTranslationService;

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
                OutputDivisions = [];
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

    [ObservableProperty]
    private string _outputText;

    [ObservableProperty]
    public JapaneseTextDivisionViewModel[] _outputDivisions;

    [ObservableProperty]
    private TranslationMicrophoneFeatureViewModel _microphoneFeature;

    [ObservableProperty]
    private TranslationClipboardFeatureViewModel _clipboardFeature;

    public TranslationViewModel()
    {
        ReloadServices(true);
        AppEnvironment.MessageReceived += OnMessageBusMessageReceived;
    }

    private void OnMessageBusMessageReceived(IAppMessageBusMessage messageInfo)
    {
        Action handler = messageInfo switch
        {
            AppMessageBusMessage<TranslationMicrophoneFeatureMessages, string> message => () => ProcessMicrophoneFeatureMessage(message),
            AppMessageBusMessage<TranslationClipboardFeatureMessages, string> message => () => ProcessClipboardFeatureMessages(message),
            _ => null,
        };

        handler?.Invoke();

        void ProcessMicrophoneFeatureMessage(AppMessageBusMessage<TranslationMicrophoneFeatureMessages, string> typedMessageInfo)
        {
            if (typedMessageInfo.Message == TranslationMicrophoneFeatureMessages.MicrophoneInputReceived)
            {
                InputText = typedMessageInfo.Value;
            }
        }

        void ProcessClipboardFeatureMessages(AppMessageBusMessage<TranslationClipboardFeatureMessages, string> typedMessageInfo)
        {
            if (typedMessageInfo.Message == TranslationClipboardFeatureMessages.ClipboardImageTextReceived)
            {
                InputText = typedMessageInfo.Value;
            }
        }
    }

    public void ReloadServices(bool isInitialLoad)
    {
        LoadingTranslationService = true;

        _ = Task.Factory.StartNew(async () =>
        {
            if (isInitialLoad)
            {
                ConfigureIpaDict();

                MicrophoneFeature = new TranslationMicrophoneFeatureViewModel();
                ClipboardFeature = new TranslationClipboardFeatureViewModel();
            }
            else
            {
                await MicrophoneFeature.StopRecordingAsync();
            }

            LoadingTranslationService = false;
        });
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

        Dispatcher.UIThread.Invoke(() =>
        {
            if (isInputJapanese)
            {
                OutputDivisions = Utilities.DivideAndColorJapaneseText(text, null);
                OutputText = response;
            }
            else
            {
                OutputDivisions = Utilities.DivideAndColorJapaneseText(response, null);
                OutputText = "";
            }
        });
    }
}