namespace IkOtomasyon.Api.Services;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
