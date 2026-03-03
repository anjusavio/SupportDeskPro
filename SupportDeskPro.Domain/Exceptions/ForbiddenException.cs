/// <summary>
/// Thrown when an authenticated user attempts an action beyond their role permissions.
/// Maps to HTTP 403 Forbidden in the global exception middleware.
/// Example: Agent attempting to access admin-only endpoints.
/// </summary>
namespace SupportDeskPro.Domain.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(
        string message = "You do not have permission to perform this action.")
        : base(message) { }
}