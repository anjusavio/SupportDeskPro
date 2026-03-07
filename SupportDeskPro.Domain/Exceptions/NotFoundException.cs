/// <summary>
/// Thrown when a requested resource cannot be found in the database.
/// Maps to HTTP 404 Not Found in the global exception middleware.
/// Example: GetTenantById — tenant with given Id does not exist.
/// </summary>
namespace SupportDeskPro.Domain.Exceptions;

[System.Diagnostics.DebuggerNonUserCode]
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with identifier '{key}' was not found.")
    {
        Entity = entity;
        Key = key;
    }

    public string Entity { get; }
    public object Key { get; }
}