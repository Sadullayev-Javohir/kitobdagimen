using FluentValidation.Results;

namespace KitobdaGimen.Application.Common.Exceptions;

/// <summary>
/// Aggregates FluentValidation failures into a single exception with a per-field error map.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException()
        : base("Bir yoki bir nechta validatsiya xatosi yuz berdi.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
