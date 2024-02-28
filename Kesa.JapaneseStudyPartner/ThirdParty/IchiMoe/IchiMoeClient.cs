using HtmlAgilityPack;
using Kesa.Japanese.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Kesa.Japanese.ThirdParty.IchiMoe;

public class IchiMoeClient
{
    private HttpClient Client { get; } = new();

    public IEnumerable<IchiMoeEntry> Query(string query)
    {
        var url = $"https://ichi.moe/cl/qr/?q={Uri.EscapeDataString(query)}&r=htr";
        var doc = Client.GetHtmlAsync(url).GetAwaiter().GetResult();

        var glosses = doc.DocumentNode.SelectNodes("//div[contains(@class, 'gloss-all')]/div[@class='row gloss-row']/ul/li");
        if (glosses == null)
        {
            yield break;
        }

        foreach (var node in glosses)
        {
            var alternativesNode = node.SelectSingleNode(".//dl[@class='alternatives']");
            if (alternativesNode != null)
            {
                var entryNodes = IchiMoeEntry.GetDataNodes(alternativesNode).FirstOrDefault();
                yield return IchiMoeEntry.Read(entryNodes.Text, entryNodes.Data);
            }
        }

        yield break;
    }
}

public class IchiMoeDefinition
{
    public string PartOfSpeech { get; set; }

    public string Description { get; set; }
}

public class IchiMoeEntry
{
    public static IEnumerable<(HtmlNode Text, HtmlNode Data)> GetDataNodes(HtmlNode node)
    {
        foreach (var childNode in node.ChildNodes)
        {
            if (childNode.NodeType == HtmlNodeType.Element && childNode.Name == "dt")
            {
                var dataNode = childNode.NextSibling.NextSibling;
                yield return (childNode, dataNode);
            }
        }
    }

    public static IEnumerable<IchiMoeDefinition> ReadDefinitions(HtmlNode definitionsNode)
    {
        if (definitionsNode?.SelectNodes("li") is { } definitionNodes)
        {
            foreach (var definitionNode in definitionNodes)
            {
                var posDesc = definitionNode.GetNodeInnerText("./span[@class='pos-desc']");
                var description = definitionNode.GetNodeInnerText("./span[@class='gloss-desc']");

                yield return new IchiMoeDefinition
                {
                    PartOfSpeech = posDesc,
                    Description = description
                };
            }
        }
    }

    public static IEnumerable<IchiMoeConjugation> ReadConjugations(HtmlNode conjugationsNode)
    {
        if (conjugationsNode?.SelectNodes("./div[@class='conjugation']") is { } conjugationNodes)
        {
            foreach (var conjugationNode in conjugationNodes)
            {
                var propertiesNode = conjugationNode.SelectSingleNode("./div[@class='conj-prop']");
                var verbType = propertiesNode.GetNodeInnerText("./span[@class='pos-desc']");
                var conjugationType = propertiesNode.GetNodeInnerText("./span[@class='conj-type']");
                var conjugationNegative = propertiesNode.GetNodeInnerText("./span[@class='conj-negative']")?.Equals("Negative") ?? false;
                var conjugation = new IchiMoeConjugation()
                {
                    VerbType = verbType,
                    ConjugationType = conjugationType,
                    IsNegative = conjugationNegative,
                };

                var conjugationGlossNodes = conjugationNode.SelectSingleNode("./div[@class='conj-gloss']/dl");
                foreach (var conjPair in GetDataNodes(conjugationGlossNodes))
                {
                    var entry = Read(conjPair.Text, conjPair.Data);
                    conjugation.Words.Add(entry);
                }

                yield return conjugation;
            }
        }
    }

    public string Text { get; set; }

    public string CompoundDescription { get; set; }

    public string SuffixDescription { get; set; }

    public List<IchiMoeConjugation> Conjugations { get; set; } = [];

    public List<IchiMoeDefinition> Definitions { get; set; } = [];

    public List<IchiMoeEntry> Compounds { get; set; } = [];

    public static IchiMoeEntry Read(HtmlNode text, HtmlNode data)
    {
        var entry = new IchiMoeEntry();

        entry.Text = text.InnerText.ToString();

        if (data.SelectSingleNode("./ol[@class='gloss-definitions']") is { } definitionsNode)
        {
            entry.Definitions.AddRange(ReadDefinitions(definitionsNode));
        }

        if (data.SelectSingleNode("./span[@class='suffix-desc']") is { } suffixNode)
        {
            entry.SuffixDescription = suffixNode.InnerText.Trim();
        }

        if (data.SelectSingleNode("./div[@class='conjugations']") is { } conjugationsNode)
        {
            entry.Conjugations.AddRange(ReadConjugations(conjugationsNode));
        }

        if (data.SelectSingleNode("./dl[@class='compounds']") is { } compoundsNode)
        {
            var compoundDescription = compoundsNode.PreviousSibling.PreviousSibling.InnerText.Trim();
            entry.CompoundDescription = compoundDescription;

            foreach (var compoundPair in GetDataNodes(compoundsNode))
            {
                var compoundEntry = Read(compoundPair.Text, compoundPair.Data);
                entry.Compounds.Add(compoundEntry);
            }
        }

        return entry;
    }
}

public class IchiMoeConjugation
{
    public string VerbType { get; set; }

    public string ConjugationType { get; set; }

    public bool IsNegative { get; set; }

    public List<IchiMoeEntry> Words { get; set; } = [];

    public IchiMoeConjugation()
    {
        Words = new List<IchiMoeEntry>();
    }
}