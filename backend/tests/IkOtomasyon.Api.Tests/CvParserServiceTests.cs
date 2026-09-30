using IkOtomasyon.Api.Services;
using NPOI.XWPF.UserModel;

namespace IkOtomasyon.Api.Tests;

public class CvParserServiceTests
{
    [Theory]
    [InlineData("Deneyim: C#, .NET, SQL ve Docker.", new[] { "C#", ".NET", "SQL", "Docker" })]
    [InlineData("Backend: C# ile API gelistirme", new[] { "C#" })]
    [InlineData("Stack: .NET 8 (C#)", new[] { ".NET", "C#" })]
    [InlineData("Node.js ve JavaScript", new[] { "Node.js", "JavaScript" })]
    public async Task ExtractsSkillsWithSymbols(string text, string[] expected)
    {
        var skills = await ParseTextAsync(text);

        foreach (var skill in expected)
        {
            Assert.Contains(skill, skills);
        }
    }

    [Theory]
    [InlineData("Javascript uzmani", "Java")]
    [InlineData("MySQLite degil, NoSQL", "SQL")]
    [InlineData("gitlab kullanicisi", "Git")]
    public async Task DoesNotMatchInsideOtherWords(string text, string unexpected)
    {
        var skills = await ParseTextAsync(text);

        Assert.DoesNotContain(unexpected, skills);
    }

    private static async Task<IReadOnlyCollection<string>> ParseTextAsync(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cv-{Guid.NewGuid():N}.docx");
        try
        {
            using (var doc = new XWPFDocument())
            {
                doc.CreateParagraph().CreateRun().SetText(text);
                using var fs = File.Create(path);
                doc.Write(fs);
            }

            var result = await new CvParserService().ParseAsync(path, "docx", "Test Aday", null);
            return result.Skills;
        }
        finally
        {
            File.Delete(path);
        }
    }
}
