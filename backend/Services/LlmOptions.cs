namespace IkOtomasyon.Api.Services;

public class LlmOptions
{
    public const string SectionName = "LLM";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "openai";
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "mock-llm-v1";
    public int TimeoutSeconds { get; set; } = 20;
}
