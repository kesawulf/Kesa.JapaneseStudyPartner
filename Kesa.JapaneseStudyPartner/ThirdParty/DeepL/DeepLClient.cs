using DeepL;
using System.Threading.Tasks;

namespace Kesa.Japanese.ThirdParty.DeepL;

public class DeepLClient
{
    private Translator _translator;
    private string _apiKey;

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;

            _translator = _apiKey != null
                ? new Translator(value)
                : null;
        }
    }

    public string Translate(string text, string fromLanguage, string toLanguage)
    {
        if (_translator == null)
        {
            //TODO: throw + handle properly
            return "DeepL API Key missing.";
        }

        return Task.Run(() => _translator.TranslateTextAsync(text, fromLanguage, toLanguage)).GetAwaiter().GetResult().Text;
    }
}