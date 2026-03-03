/// <summary>
/// Thrown when a business rule is violated during command/query handling.
/// Maps to HTTP 400 Bad Request in the global exception middleware.
/// Example: Inviting an agent with an email that already exists in the tenant.
/// </summary>
namespace SupportDeskPro.Domain.Exceptions;

public class BusinessValidationException : Exception
{
    public BusinessValidationException(string message) : base(message) { }

    public BusinessValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string? Field { get; }
}