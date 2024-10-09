using Google.Cloud.Speech.V1;
using Google.Cloud.Vision.V1;
using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Main;
using Kesa.Japanese.ThirdParty.Anki;
using Kesa.Japanese.ThirdParty.DeepL;
using Kesa.Japanese.ThirdParty.IchiMoe;
using Kesa.Japanese.ThirdParty.Jisho;
using Kesa.WaniKaniApi;
using System;
using System.Net.Http;

namespace Kesa.Japanese.Features;

internal interface IAppMessageBusMessage;

internal class AppMessageBusMessage<TMessage> : IAppMessageBusMessage
{
    public TMessage Message { get; set; }
}

internal class AppMessageBusMessage<TMessage, TValue> : IAppMessageBusMessage
{
    public TMessage Message { get; set; }

    public TValue Value { get; set; }
}

internal static class AppEnvironment
{
    public static event Action Initialized;

    public static event Action<IAppMessageBusMessage> MessageReceived;

    private static bool _isFirstInitialization = true;

    public static bool IsInitialized => _isFirstInitialization == false;

    #region Third Party APIs
    public static HttpClient HttpClient { get; set; }

    public static DeepLClient DeepLClient { get; private set; }

    public static ImageAnnotatorClient AnnotationClient { get; private set; }

    public static SpeechClient SpeechClient { get; private set; }

    public static JishoClient JishoClient { get; private set; }

    public static IchiMoeClient IchiMoeClient { get; private set; }

    public static AnkiConnectClient AnkiConnectClient { get; private set; }

    public static WaniKaniClient WaniKaniClient { get; private set; }
    #endregion

    public static KeyedDebounce Debounce { get; private set; }

    public static MainWindow MainWindow { get; private set; }

    public static MainWindowViewModel MainWindowViewModel => MainWindow.DataContext as MainWindowViewModel;

    public static AppSettings Settings { get; private set; }

    public static void Initialize(MainWindow mainWindow)
    {
        var wasFirstInitialization = false;

        if (_isFirstInitialization)
        {
            _isFirstInitialization = false;
            wasFirstInitialization = true;

            MainWindow = mainWindow;

            Debounce = new KeyedDebounce();
            Settings = new AppSettings();
            HttpClient = new HttpClient();
            DeepLClient = new DeepLClient();
            JishoClient = new JishoClient();
            IchiMoeClient = new IchiMoeClient();
            AnkiConnectClient = new AnkiConnectClient();
        }

        Settings.Reload();

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Settings.GoogleCredentialsFilePath);

        AnnotationClient = ImageAnnotatorClient.Create();
        SpeechClient = SpeechClient.Create();
        WaniKaniClient = new WaniKaniClient(HttpClient, Settings.WaniKaniApiKey);

        DeepLClient.ApiKey = Settings.DeepLApiKey;

        if (wasFirstInitialization)
        {
            Initialized?.Invoke();
        }
        else
        {
            MainWindowViewModel.TranslationViewModel?.ReloadServices(false);
        }
    }

    public static void SendMessage<TMessage>(TMessage message)
    {
        MessageReceived?.Invoke(new AppMessageBusMessage<TMessage>() { Message = message });
    }

    public static void SendMessage<TMessage, TValue>(TMessage messageType, TValue value)
    {
        var message = new AppMessageBusMessage<TMessage, TValue>() { Message = messageType, Value = value };
        MessageReceived?.Invoke(message);
    }
}