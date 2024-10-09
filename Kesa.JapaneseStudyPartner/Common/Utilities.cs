using Avalonia.Controls.Documents;
using Avalonia.Media;
using HtmlAgilityPack;
using Kawazu;
using Kesa.Japanese.Features.Sentences;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
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

    public static KawazuConverter JapanesePronunciationConverter { get; } = new();

    public static string GetJapanesePronunciation(this string japaneseText)
    {
        return Task.Run(() => JapanesePronunciationConverter.Convert(japaneseText)).Result;
    }

    public static async Task<string> ToRubyTextAsync(string text)
    {
        var divisions = await JapanesePronunciationConverter.GetDivisions(text);
        var sb = new StringBuilder();

        foreach (var division in divisions)
        {
            foreach (var element in division)
            {
                var value = element.Type switch
                {
                    TextType.PureKana or TextType.Others => element.Element,
                    _ => $" {element.Element}[{element.HiraPronunciation}]",
                };

                sb.Append(value);
            }
        }

        return sb.ToString();
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

    private static readonly ThreadLocal<StringBuilder> _joinStringBuiler = new ThreadLocal<StringBuilder>(static () => new StringBuilder());

    public static string JoinToString(this IEnumerable<string> source)
    {
        var value = _joinStringBuiler.Value;

        foreach (var item in source)
        {
            value.Append(item);
        }

        var result = value.ToString();
        value.Clear();
        return result;
    }

    public static JapaneseTextDivisionViewModel[] DivideAndColorJapaneseText(string japaneseText, string textToHighlight)
    {
        var items = divideText(japaneseText).ToArray();
        highlightDivisions(items, textToHighlight);

        var japanese = items.Select(pair => pair.Japanese).ToArray();
        var pronunciation = items.Select(pair => pair.Pronunciation).ToArray();

        return japanese
            .Zip(pronunciation)
            .Select(pair =>
            {
                var vm = new JapaneseTextDivisionViewModel();
                vm.Foreground = pair.First.Foreground;
                vm.JapaneseText = pair.First.Text;
                vm.FuriganaText = pair.First.Text.Any(c => c.IsKanji()) ? pair.Second.Text : "";
                return vm;
            })
            .ToArray();

        static IEnumerable<(Run Japanese, Run Pronunciation)> divideText(string text)
        {
            foreach (var division in Task.Run(() => Utilities.JapanesePronunciationConverter.GetDivisions(text)).GetAwaiter().GetResult())
            {
                Trace.WriteLine(division.PartsOfSpeech);

                foreach (var elementInfo in division)
                {
                    string colorCode = division.PartsOfSpeech switch
                    {
                        "接頭詞" => "#D9C982", //prefix (honorifics, etc)
                        "接続詞" => "#82D9B6", //conjunction
                        "連体詞" or "形容詞" => "#73BFB8", //adjectives
                        "名詞" => "#829CD9", //noun
                        "助詞" => "#82B5D9", //particle
                        "動詞" => "#D982D2", //verbs
                        "助動詞" => "#D98DD2", //auxiliary verbs
                        "副詞" => "#D977CF", //adverbs
                        "感動詞" => "#D982C6", //interjections (uhm, uhh, えっと)
                        "記号" => "#FFFFFF", //symbols
                        _ => "#FFFFFF",
                    };

                    SolidColorBrush colorBrush = default;

                    if (colorCode is { })
                    {
                        colorBrush = new SolidColorBrush(Color.Parse(colorCode));

                        yield return (
                            new Run(elementInfo.Element) { Foreground = colorBrush },
                            new Run(elementInfo.HiraNotation) { Foreground = colorBrush }
                        );
                    }
                    else
                    {
                        yield return (
                            new Run(elementInfo.Element),
                            new Run(elementInfo.HiraNotation)
                        );
                    }
                }
            }
        }

        static void highlightDivisions((Run Japanese, Run Pronunciation)[] items, string searchTerm)
        {
            for (int i = 1; i < items.Length; i++)
            {
                foreach (var window in items.Window(i))
                {
                    var japaneseText = window.Select(item => item.Japanese.Text).JoinToString();
                    var pronunciationText = window.Select(item => item.Pronunciation.Text).JoinToString();

                    if (searchTerm != null && (japaneseText.Contains(searchTerm) || pronunciationText.Contains(searchTerm)))
                    {
                        foreach (var pair in window)
                        {
                            pair.Japanese.Foreground = new SolidColorBrush(Color.Parse("#F08080"));
                            pair.Pronunciation.Foreground = new SolidColorBrush(Color.Parse("#F08080"));
                        }

                        return;
                    }
                }
            }
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

    public static (PropertyInfo Property, Func<TParent, TProperty> Getter, Action<TParent, TProperty> Setter) GetGetterAndSetter<TParent, TProperty>(Expression<Func<TParent, TProperty>> expression)
    {
        var currentExpression = expression.Body;
        while (currentExpression.NodeType is ExpressionType.Convert or ExpressionType.TypeAs)
        {
            currentExpression = ((UnaryExpression)currentExpression).Operand;
        }

        if (currentExpression.NodeType != ExpressionType.MemberAccess)
        {
            throw new ArgumentException();
        }

        var property = (MemberExpression)currentExpression;
        currentExpression = property.Expression;

        while (currentExpression.NodeType != ExpressionType.Parameter)
        {
            if (currentExpression.NodeType is ExpressionType.Convert or ExpressionType.TypeAs)
            {
                currentExpression = ((UnaryExpression)currentExpression).Operand;
            }
            else if (currentExpression.NodeType == ExpressionType.MemberAccess)
            {
                currentExpression = ((MemberExpression)currentExpression).Expression;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        var propertyInfo = (PropertyInfo)property.Member;
        var setterFunc = propertyInfo.GetSetMethod() is { } setter
            ? Delegate.CreateDelegate(typeof(Action<TParent, TProperty>), setter) as Action<TParent, TProperty>
            : null;
        var getterFunc = expression.Compile();

        return (propertyInfo, getterFunc, setterFunc);
    }

    public static IEnumerable<(TKey Key, TValue Value)> Flatten<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary)
        => Flatten<TKey, List<TValue>, TValue>(dictionary);

    public static IEnumerable<(TKey Key, TValue Value)> Flatten<TKey, TValue>(this Dictionary<TKey, TValue[]> dictionary)
        => Flatten<TKey, TValue[], TValue>(dictionary);

    private static IEnumerable<(TKey Key, TValue Value)> Flatten<TKey, TEnumerable, TValue>(this Dictionary<TKey, TEnumerable> dictionary)
        where TEnumerable : IEnumerable<TValue>
    {
        foreach (var pair in dictionary)
        {
            foreach (var item in pair.Value)
            {
                yield return (pair.Key, item);
            }
        }
    }

    public static Dictionary<TKey, TSource[]> ToDictionaryByGroup<TKey, TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return ToDictionaryByGroup(source, keySelector, value => value);
    }

    public static Dictionary<TKey, TValue[]> ToDictionaryByGroup<TKey, TValue, TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
    {
        return source.ToLookup(keySelector).ToDictionary(group => group.Key, group => group.Select(valueSelector).ToArray());
    }
}