/// <summary>
/// Thrown when a duplicate or conflicting resource already exists.
/// Maps to HTTP 409 Conflict in the global exception middleware.
/// Example: Creating a tenant with a slug that already exists.
/// </summary>
namespace SupportDeskPro.Domain.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}