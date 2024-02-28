using HtmlAgilityPack;
using Kawazu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Kesa.Japanese.Common;

public static class Utilities
{
    public static bool HasJapaneseText(this string text)
        => text.Any(chr => chr.IsKanji() || chr.IsHiragana() || chr.IsKatakana());

    public static bool IsHiragana(this char character)
        => (int)character is >= 0x3040 and <= 0x309F;

    public static bool IsKatakana(this char character)
        => (int)character is >= 0x30A0 and <= 0x30FF;

    public static bool IsKanji(this char character)
        => (int)character is >= 0x4E00 and <= 0x9FBF;

    private static KawazuConverter JapanesePronunciationConverter { get; } = new();

    public static string GetJapanesePronunciation(this string japaneseText)
    {
        return Task.Run(() => JapanesePronunciationConverter.Convert(japaneseText)).Result;
    }

    public static Stream GetEmbeddedResourceStream(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith("." + path, StringComparison.CurrentCultureIgnoreCase));

        return resourceName != null
            ? assembly.GetManifestResourceStream(resourceName)
            : null;
    }

    public static byte[] ReadToByteArray(this Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static async Task<HtmlDocument> GetHtmlAsync(this HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    public static IEnumerable<string> GetLines(this string value)
    {
        using var reader = new StringReader(value);

        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    public static HtmlNodeCollection SelectHtmlNodes(this HtmlNode node, string tag, string attribute, string value) => node.SelectNodes($"{tag}[contains(@{attribute}, '{value}')]");

    public static HtmlNode SelectSingleHtmlNode(this HtmlNode node, string tag, string attribute, string value) => node.SelectSingleNode($"{tag}[contains(@{attribute}, '{value}')]");

    public static HtmlNodeCollection SelectDivs(this HtmlNode node, string className) => node.SelectHtmlNodes("div", "class", className);

    public static HtmlNode SelectSingleDiv(this HtmlNode node, string className) => node.SelectSingleHtmlNode("div", "class", className);

    public static HtmlNodeCollection SelectHtmlNodesExact(this HtmlNode node, string tag, string attribute, string value) => node.SelectNodes($"{tag}[@{attribute}='{value}']");

    public static HtmlNode SelectSingleHtmlNodeExact(this HtmlNode node, string tag, string attribute, string value) => node.SelectSingleNode($"{tag}[@{attribute}='{value}']");

    public static HtmlNodeCollection SelectDivsExact(this HtmlNode node, string className) => node.SelectHtmlNodesExact("div", "class", className);

    public static HtmlNode SelectSingleDivExact(this HtmlNode node, string className) => node.SelectSingleHtmlNodeExact("div", "class", className);

    public static string GetNodeInnerText(this HtmlNode node, string xpath) => node?.SelectSingleNode(xpath)?.InnerText.Trim();
}