namespace IkOtomasyon.Api.Services;

public static class SkillNormalization
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["js"] = "JavaScript",
        ["javascript"] = "JavaScript",
        ["dotnet"] = ".NET",
        [".net"] = ".NET",
        ["nodejs"] = "Node.js",
        ["node.js"] = "Node.js",
        ["ts"] = "TypeScript"
    };

    public static string Normalize(string skill)
    {
        var raw = skill.Trim();
        if (raw.Length == 0)
        {
            return raw;
        }

        return Map.TryGetValue(raw, out var mapped) ? mapped : raw;
    }
}
