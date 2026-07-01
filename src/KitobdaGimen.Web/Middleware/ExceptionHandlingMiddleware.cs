using System.Text.Json;
using KitobdaGimen.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using ValidationException = KitobdaGimen.Application.Common.Exceptions.ValidationException;

namespace KitobdaGimen.Web.Middleware;

/// <summary>
/// Translates Application-layer exceptions into JSON HTTP responses with appropriate status codes.
/// UI messages are in Uzbek.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, message, errors) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, ve.Message, ve.Errors),
            NotFoundException nf => (StatusCodes.Status404NotFound, nf.Message, null),
            ForbiddenAccessException fa => (StatusCodes.Status403Forbidden, fa.Message, null),
            UnauthorizedAccessException ua => (StatusCodes.Status401Unauthorized, ua.Message, null),
            DbUpdateException db => (StatusCodes.Status400BadRequest,
                "Ma'lumotlar bazasida xatolik: " + ExtractDbErrorMessage(db),
                (IDictionary<string, string[]>?)null),
            _ => (StatusCodes.Status500InternalServerError, "Serverda kutilmagan xatolik yuz berdi.",
                (IDictionary<string, string[]>?)null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Ishlov berilmagan xatolik");
        }
        else if (exception is DbUpdateException)
        {
            // DB write failures are logged at Error with the full inner detail so the exact
            // failing constraint/table is always visible in the server logs (past user-delete
            // FK bugs were hard to diagnose because the sanitized client message hid the cause).
            _logger.LogError(exception, "Ma'lumotlar bazasi yozuv xatosi: {Detail}",
                exception.InnerException?.Message ?? exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "So'rov xatosi: {Message}", exception.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = JsonSerializer.Serialize(new { message, errors }, JsonOptions);
        await context.Response.WriteAsync(payload);
    }

    private static string ExtractDbErrorMessage(DbUpdateException ex)
    {
        // Try to extract a user-friendly message from the inner exception.
        var inner = ex.InnerException?.Message ?? ex.Message;

        if (inner.Contains("foreign key constraint", StringComparison.OrdinalIgnoreCase) ||
            inner.Contains("FK_", StringComparison.OrdinalIgnoreCase))
        {
            // Name the offending table so a missed dependency is immediately actionable
            // instead of hiding behind a generic message (e.g. "... jadval: Comments").
            var table = ExtractReferencedTable(inner);
            return table is null
                ? "Bog'liq ma'lumotlar mavjud. Avval bog'langan yozuvlarni o'chiring."
                : $"Bog'liq ma'lumotlar mavjud (jadval: {table}). Avval bog'langan yozuvlarni o'chiring.";
        }

        if (inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
            inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            return "Bunday qiymat allaqachon mavjud.";
        }

        // Return a sanitized version (don't expose full SQL error to users).
        return "Ma'lumotlarni saqlashda xatolik yuz berdi.";
    }

    /// <summary>
    /// Pulls the child table name out of a PostgreSQL FK-violation message such as
    /// <c>... violates foreign key constraint "FK_Comments_Users_UserId" on table "Comments"</c>.
    /// Returns <c>null</c> if no table name can be found.
    /// </summary>
    private static string? ExtractReferencedTable(string message)
    {
        const string marker = "on table \"";
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }
        start += marker.Length;
        var end = message.IndexOf('"', start);
        return end > start ? message[start..end] : null;
    }
}
