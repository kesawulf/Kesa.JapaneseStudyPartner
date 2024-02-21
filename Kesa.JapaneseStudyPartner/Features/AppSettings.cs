using Newtonsoft.Json;
using System;
using System.IO;

namespace Kesa.Japanese.Features;

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