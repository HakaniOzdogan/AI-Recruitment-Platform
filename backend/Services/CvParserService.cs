using System.Globalization;
using System.Text.RegularExpressions;
using NPOI.XWPF.UserModel;
using UglyToad.PdfPig;

namespace IkOtomasyon.Api.Services;

public class CvParserService
{
    private static readonly Regex UrlRegex = new("https?://[^\\s)\\]>\\\"']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DomainLinkRegex = new("\\b(?:github\\.com|linkedin\\.com|gitlab\\.com|behance\\.net|medium\\.com)(?:/[^\\s)\\]>\\\"']*)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateRangeRegex = new(@"(?<start>(0?[1-9]|1[0-2])[./-]\d{4}|\d{4})\s*[-–]\s*(?<end>(0?[1-9]|1[0-2])[./-]\d{4}|\d{4}|present|current|now)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> SkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c#"] = "C#",
        ["dotnet"] = ".NET",
        [".net"] = ".NET",
        ["java"] = "Java",
        ["sql"] = "SQL",
        ["react"] = "React",
        ["docker"] = "Docker",
        ["kubernetes"] = "Kubernetes",
        ["aws"] = "AWS",
        ["azure"] = "Azure",
        ["git"] = "Git",
        ["js"] = "JavaScript",
        ["javascript"] = "JavaScript",
        ["nodejs"] = "Node.js",
        ["node.js"] = "Node.js"
    };

    private static readonly Dictionary<string, string> LanguageKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["english"] = "English",
        ["ingilizce"] = "English",
        ["turkish"] = "Turkish",
        ["turkce"] = "Turkish",
        ["türkçe"] = "Turkish",
        ["german"] = "German",
        ["almanca"] = "German",
        ["french"] = "French",
        ["fransizca"] = "French",
        ["fransızca"] = "French"
    };

    public async Task<ParsedCvResult> ParseAsync(string filePath, string fileType, string fallbackName, string? fallbackEmail, CancellationToken ct = default)
    {
        var text = await ExtractTextAsync(filePath, fileType, ct);
        var normalized = text.Replace('\r', ' ').Replace('\n', ' ');

        var skills = ExtractSkills(normalized);
        var links = ExtractLinks(normalized);
        var languages = ExtractLanguages(normalized);
        var experienceMonths = EstimateExperienceMonths(normalized);
        var email = EmailRegex.Match(normalized).Success ? EmailRegex.Match(normalized).Value : fallbackEmail;
        if (links.Count == 0 && !string.IsNullOrWhiteSpace(email))
        {
            links.Add($"mailto:{email}");
        }
        var summary = string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized[..Math.Min(500, normalized.Length)].Trim();

        return new ParsedCvResult(
            FullNameSnapshot: fallbackName,
            EmailSnapshot: email,
            Summary: summary,
            TotalExperienceMonths: experienceMonths,
            Skills: skills,
            Education: Array.Empty<string>(),
            Experience: Array.Empty<string>(),
            Languages: languages,
            Links: links);
    }

    private static async Task<string> ExtractTextAsync(string filePath, string fileType, CancellationToken ct)
    {
        return fileType.ToLowerInvariant() switch
        {
            "pdf" => await Task.Run(() => ExtractPdf(filePath), ct),
            "docx" => await Task.Run(() => ExtractDocx(filePath), ct),
            "doc" => throw new InvalidOperationException("Legacy .doc parsing is not supported in MVP."),
            _ => throw new InvalidOperationException("Unsupported file type.")
        };
    }

    private static string ExtractPdf(string filePath)
    {
        using var doc = PdfDocument.Open(filePath);
        return string.Join("\n", doc.GetPages().Select(x => x.Text));
    }

    private static string ExtractDocx(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var doc = new XWPFDocument(fs);
        return string.Join("\n", doc.Paragraphs.Select(p => p.ParagraphText));
    }

    private static List<string> ExtractSkills(string text)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in SkillAliases)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(alias.Key)}\b", RegexOptions.IgnoreCase))
            {
                found.Add(alias.Value);
            }
        }

        return found.OrderBy(x => x).ToList();
    }

    private static List<string> ExtractLanguages(string text)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in LanguageKeywords)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(item.Key)}\b", RegexOptions.IgnoreCase))
            {
                found.Add(item.Value);
            }
        }

        return found.OrderBy(x => x).ToList();
    }

    private static List<string> ExtractLinks(string text)
    {
        var links = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in UrlRegex.Matches(text))
        {
            links.Add(match.Value.TrimEnd('.', ',', ';'));
        }

        foreach (Match match in DomainLinkRegex.Matches(text))
        {
            var value = match.Value.TrimEnd('.', ',', ';');
            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = $"https://{value}";
            }

            links.Add(value);
        }

        return links.OrderBy(x => x).ToList();
    }

    private static int? EstimateExperienceMonths(string text)
    {
        var ranges = DateRangeRegex.Matches(text);
        if (ranges.Count == 0)
        {
            return null;
        }

        var total = 0;
        foreach (Match range in ranges)
        {
            if (!TryParseMonth(range.Groups["start"].Value, out var start))
            {
                continue;
            }

            var endRaw = range.Groups["end"].Value;
            DateTime end;
            if (Regex.IsMatch(endRaw, "present|current|now", RegexOptions.IgnoreCase))
            {
                end = DateTime.UtcNow;
            }
            else if (!TryParseMonth(endRaw, out end))
            {
                continue;
            }

            var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
            if (months > 0)
            {
                total += months;
            }
        }

        return total == 0 ? null : total;
    }

    private static bool TryParseMonth(string raw, out DateTime value)
    {
        value = default;
        raw = raw.Trim();

        if (DateTime.TryParseExact(raw, new[] { "M/yyyy", "MM/yyyy", "M.yyyy", "MM.yyyy", "M-yyyy", "MM-yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthYear))
        {
            value = new DateTime(monthYear.Year, monthYear.Month, 1);
            return true;
        }

        if (DateTime.TryParseExact(raw, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yearOnly))
        {
            value = new DateTime(yearOnly.Year, 1, 1);
            return true;
        }

        return false;
    }
}

