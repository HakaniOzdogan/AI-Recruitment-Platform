namespace IkOtomasyon.Api.Services;

public class InvalidStateTransitionException : Exception
{
    public InvalidStateTransitionException(string message) : base(message)
    {
    }
}
