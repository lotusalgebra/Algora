namespace Algora.Domain.Entities;

/// <summary>
/// Stores application-wide configuration settings in the database.
/// </summary>
public class AppConfiguration
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}