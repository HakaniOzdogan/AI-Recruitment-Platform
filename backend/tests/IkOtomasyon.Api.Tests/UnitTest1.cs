using System.Reflection;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Services;

namespace IkOtomasyon.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void ExtractEvidence_TurkishTranscript_FindsProblemSolvingEvidence()
    {
        var messages = new List<string>
        {
            "Projede çözüm ürettim, analiz yaptım ve ciddi optimizasyon uyguladım."
        };

        var method = typeof(InterviewScoringService).GetMethod(
            "ExtractEvidence",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method!.Invoke(
            null,
            new object?[] { messages, RubricKeys.ProblemSolving, null });

        var evidence = Assert.IsAssignableFrom<List<EvidenceQuoteResponse>>(result);
        Assert.NotEmpty(evidence);
    }
}
