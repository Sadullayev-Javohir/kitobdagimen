namespace KitobdaGimen.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides an integer surrogate key.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
