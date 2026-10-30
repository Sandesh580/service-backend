namespace LoginCheck.Domain.Entities;

public sealed class ServiceNotification
{
    public int Id { get; set; }
    public int RecipientUserId { get; set; }
    public int? ServiceRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}