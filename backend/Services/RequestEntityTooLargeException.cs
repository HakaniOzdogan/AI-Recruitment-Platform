namespace IkOtomasyon.Api.Services;

public class RequestEntityTooLargeException : Exception
{
    public RequestEntityTooLargeException(string message) : base(message)
    {
    }
}
