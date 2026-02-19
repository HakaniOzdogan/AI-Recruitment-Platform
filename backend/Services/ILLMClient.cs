namespace IkOtomasyon.Api.Services;

public interface ILLMClient
{
    Task<string> GenerateStructuredAsync(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken ct = default);
}
