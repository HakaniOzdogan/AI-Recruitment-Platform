namespace IkOtomasyon.Api.Services;

public class LlmTimeoutException : Exception
{
    public LlmTimeoutException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public class LlmSchemaException : Exception
{
    public LlmSchemaException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public class LlmProviderException : Exception
{
    public LlmProviderException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
