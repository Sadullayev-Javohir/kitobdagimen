namespace KitobdaGimen.Application.Common.Exceptions;

/// <summary>Thrown when a requested entity does not exist.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"\"{name}\" ({key}) topilmadi.")
    {
    }
}
