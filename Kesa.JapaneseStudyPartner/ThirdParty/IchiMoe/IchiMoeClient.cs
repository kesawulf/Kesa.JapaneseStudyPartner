using HtmlAgilityPack;
using Kesa.Japanese.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Kesa.Japanese.ThirdParty.IchiMoe;

public class IchiMoeClient
{
    private HttpClient Client { get; } = new();

    public IEnumerable<IchiMoeGloss> Query(string query)
    {
        var url = $"https://ichi.moe/cl/qr/?q={Uri.EscapeDataString(query)}&r=htr";
        var doc = Client.GetHtmlAsync(url).GetAwaiter().GetResult();

        var glosses = doc.DocumentNode.SelectNodes("//div[contains(@class, 'gloss-all')]/div[@class='row gloss-row']/ul/li");

        foreach (var node in glosses)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                yield return ParseGloss(node);
            }
        }

        yield break;
    }

    public IchiMoeGloss ParseGloss(HtmlNode glossEntryNode)
    {
        if (glossEntryNode != null)
        {
            var entry = new IchiMoeGloss
            {
                Id = glossEntryNode.GetAttributeValue("id", ""),
                Word = glossEntryNode.SelectSingleNode(".//a[@class='info-link']/em")?.InnerText
            };

            var alternativesNode = glossEntryNode.SelectNodes(".//dl[@class='alternatives']/dt");
            if (alternativesNode != null)
            {
                foreach (var alternativeNode in alternativesNode)
                {
                    entry.Alternatives.Add(alternativeNode.InnerText.Trim());
                }
            }

            var definitionsNode = glossEntryNode.SelectSingleNode(".//ol[@class='gloss-definitions']");
            var definitionsNodeChildren = definitionsNode?.SelectNodes("li");
            if (definitionsNodeChildren != null)
            {
                foreach (var definitionNode in definitionsNodeChildren)
                {
                    var posDesc = definitionNode.SelectSingleNode(".//span[@class='pos-desc']").InnerText;
                    var description = definitionNode.SelectSingleNode(".//span[@class='gloss-desc']").InnerText;
                    entry.Definitions.Add(new IchiMoeGlossDefinition
                    {
                        PartOfSpeech = posDesc,
                        Description = description
                    });
                }
            }

            var conjugationsNode = glossEntryNode.SelectSingleNode(".//div[@class='conjugations']");
            var conjugationsNodeChildren = conjugationsNode?.SelectNodes(".//div[@class='conjugation']");
            if (conjugationsNodeChildren != null)
            {
                foreach (var conjugationNode in conjugationsNodeChildren)
                {
                    var verbType = conjugationNode.SelectSingleNode(".//span[@class='pos-desc']").InnerText;
                    var conjugationType = conjugationNode.SelectSingleNode(".//span[@class='conj-type']").InnerText;
                    var conjugation = new IchiMoeConjugation
                    {
                        VerbType = verbType,
                        ConjugationType = conjugationType
                    };

                    var conjugationDefinitionsNode = conjugationNode.SelectSingleNode(".//ol[@class='gloss-definitions']");
                    if (conjugationDefinitionsNode != null)
                    {
                        foreach (var definitionNode in conjugationDefinitionsNode.SelectNodes("li"))
                        {
                            var posDesc = definitionNode.SelectSingleNode(".//span[@class='pos-desc']").InnerText;
                            var description = definitionNode.SelectSingleNode(".//span[@class='gloss-desc']").InnerText;
                            conjugation.Definitions.Add(new IchiMoeGlossDefinition
                            {
                                PartOfSpeech = posDesc,
                                Description = description
                            });
                        }
                    }

                    entry.Conjugations.Add(conjugation);
                }
            }

            return entry;
        }

        return null;
    }
}
public class IchiMoeGloss
{
    public string Id { get; set; }

    public string Word { get; set; }

    public List<IchiMoeGlossDefinition> Definitions { get; set; } = [];

    public List<IchiMoeConjugation> Conjugations { get; set; } = [];

    public List<string> Alternatives { get; set; } = [];

    public IchiMoeGloss()
    {
    }
}

public class IchiMoeGlossDefinition
{
    public string PartOfSpeech { get; set; }

    public string Description { get; set; }
}

public class IchiMoeConjugation
{
    public string VerbType { get; set; }

    public string ConjugationType { get; set; }

    public List<IchiMoeGlossDefinition> Definitions { get; set; }

    public IchiMoeConjugation()
    {
        Definitions = new List<IchiMoeGlossDefinition>();
    }
}