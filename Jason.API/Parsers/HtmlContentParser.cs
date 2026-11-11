using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jason.Models.Exchange;

namespace Jason.API.Parsers;

public class HtmlContentParser : IContentParser
{
    // Títulos de navegación que no deben convertirse en fichas.
    private static readonly HashSet<string> IgnoredTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hacker News", "past", "comments", "show", "jobs", "submit", "login", "more",
        "guidelines", "faq", "lists", "api", "security", "legal", "apply to yc"
    };

    // Busca etiquetas con la forma <a href="...">Título</a>.
    private static readonly Regex AnchorPattern = new(
        "<a\\b(?<attributes>[^>]*?)\\bhref\\s*=\\s*[\"'](?<href>[^\"']+)[\"'](?<tail>[^>]*)>(?<title>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Hacker News coloca los titulares dentro de un span con la clase titleline.
    private static readonly Regex TitleLinePattern = new(
        "<span\\b[^>]*class\\s*=\\s*[\"'][^\"']*\\btitleline\\b[^\"']*[\"'][^>]*>(?<body>.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public string Format => "html";

    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
    {
        List<ExportItem> result = new();
        List<Match> links = FindContentLinks(content);
        HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> usedLinks = new(StringComparer.OrdinalIgnoreCase);
        DateTime capturedAt = DateTime.UtcNow;

        foreach (Match link in links)
        {
            string title = ContentParserUtilities.PlainText(link.Groups["title"].Value);
            string href = WebUtility.HtmlDecode(link.Groups["href"].Value.Trim());

            if (!IsUsefulLink(title, href))
                continue;

            string? url = ResolveUrl(source.Url, href);
            if (url == null || usedLinks.Contains(url))
                continue;

            usedLinks.Add(url);
            string id = ContentParserUtilities.StableId(source.Id + "|" + url + "|" + title);

            ExportItem item = new()
            {
                Id = ContentParserUtilities.UniqueId(id, usedIds),
                Title = title,
                MediaType = ContentParserUtilities.NormalizeMediaType(null, url),
                CreatedAt = capturedAt,
                Category = ContentParserUtilities.NormalizeCategory(null, source),
                Url = url,
                Data = JsonSerializer.SerializeToElement(new
                {
                    html = link.Value,
                    href = href,
                    title = title
                })
            };

            result.Add(item);

            // Evita devolver cientos de enlaces de una página muy grande.
            if (result.Count == 50)
                break;
        }

        return result;
    }

    private static bool IsUsefulLink(string title, string href)
    {
        if (title.Length < 4 || IgnoredTitles.Contains(title))
            return false;
        if (href.StartsWith("#"))
            return false;
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return false;
        if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static List<Match> FindContentLinks(string content)
    {
        List<Match> preferredLinks = new();

        // Si es Hacker News, toma solamente los enlaces marcados como titulares.
        foreach (Match titleLine in TitleLinePattern.Matches(content))
        {
            Match link = AnchorPattern.Match(titleLine.Groups["body"].Value);
            if (link.Success)
                preferredLinks.Add(link);
        }

        if (preferredLinks.Count > 0)
            return preferredLinks;

        // Para otras páginas HTML, usa todos los enlaces encontrados.
        List<Match> allLinks = new();
        foreach (Match link in AnchorPattern.Matches(content))
            allLinks.Add(link);
        return allLinks;
    }

    private static string? ResolveUrl(string? baseUrl, string href)
    {
        Uri? absoluteUrl;
        if (Uri.TryCreate(href, UriKind.Absolute, out absoluteUrl))
        {
            if (absoluteUrl.Scheme == "http" || absoluteUrl.Scheme == "https")
                return absoluteUrl.ToString();
            return null;
        }

        Uri? rootUrl;
        Uri? completeUrl;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out rootUrl)
            && Uri.TryCreate(rootUrl, href, out completeUrl)
            && (completeUrl.Scheme == "http" || completeUrl.Scheme == "https"))
        {
            return completeUrl.ToString();
        }

        return null;
    }
}
