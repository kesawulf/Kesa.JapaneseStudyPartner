using Google.Cloud.Speech.V1;
using Google.Cloud.Vision.V1;
using Kesa.Japanese.Common;
using Kesa.Japanese.Features.Main;
using Kesa.Japanese.ThirdParty.DeepL;
using Kesa.Japanese.ThirdParty.Jisho;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Kesa.Japanese.Features;

internal static class AppEnvironment
{
    public static event Action Initialized;

    private static bool _isFirstInitialization = true;

    public static bool IsInitialized => _isFirstInitialization == false;

    #region Third Party APIs
    public static DeepLClient DeepLClient { get; private set; }

    public static ImageAnnotatorClient AnnotationClient { get; private set; }

    public static SpeechClient SpeechClient { get; private set; }

    public static JishoClient JishoClient { get; private set; }
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
            DeepLClient = new DeepLClient();
            JishoClient = new JishoClient();
        }

        Settings.Reload();

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Settings.GoogleCredentialsFilePath);

        AnnotationClient = ImageAnnotatorClient.Create();
        SpeechClient = SpeechClient.Create();

        DeepLClient.ApiKey = Settings.DeepLApiKey;

        ClipboardWatcher.Initialize();

        if (wasFirstInitialization)
        {
            Initialized?.Invoke();
        }
    }
}

internal class AppSettings
{
    public string GoogleCredentialsFilePath { get; set; }

    public string WaniKaniApiKey { get; set; }

    public string DeepLApiKey { get; set; }

    public string SaplingApiKey { get; set; }

    public AppSettings() { }

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "Kesa.JapaneseStudyPartner", "settings.json");

    public void Reload()
    {
        try
        {
            EnsureDirectoryExists();
            var json = File.ReadAllText(FilePath);
            var data = JsonConvert.DeserializeObject<AppSettings>(json);

            GoogleCredentialsFilePath = data.GoogleCredentialsFilePath;
            WaniKaniApiKey = data.WaniKaniApiKey;
            DeepLApiKey = data.DeepLApiKey;
            SaplingApiKey = data.SaplingApiKey;
        }
        catch
        {
            //Do nothing
        }

        GoogleCredentialsFilePath ??= "";
        WaniKaniApiKey ??= "";
        DeepLApiKey ??= "";
        SaplingApiKey ??= "";
    }

    public void Save()
    {
        try
        {
            EnsureDirectoryExists();
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            //Do nothing
        }
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
    }
}