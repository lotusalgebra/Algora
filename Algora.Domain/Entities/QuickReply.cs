namespace Algora.Domain.Entities;

/// <summary>
/// Represents a pre-defined quick reply template for fast responses.
/// </summary>
public class QuickReply
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this quick reply belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the quick reply.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The reply content/message template.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category for organization: greeting, shipping, returns, general.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Keyboard shortcut: /thanks, /shipping, etc.
    /// </summary>
    public string? Shortcut { get; set; }

    /// <summary>
    /// Number of times this reply has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether this quick reply is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the quick reply was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
