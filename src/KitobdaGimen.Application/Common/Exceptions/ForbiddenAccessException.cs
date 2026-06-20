namespace KitobdaGimen.Application.Common.Exceptions;

/// <summary>Thrown when the current user is not allowed to perform an action.</summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Bu amalni bajarishga ruxsatingiz yo'q.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
