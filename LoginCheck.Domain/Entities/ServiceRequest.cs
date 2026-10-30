namespace LoginCheck.Domain.Entities;

public class ServiceRequest
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Appliance { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;

    // Status: New | AwaitingParts | Scheduled | InProgress | Completed | PendingApproval
    public string Status { get; set; } = "New";

    // Priority: - (not set) | High | Medium | Low
    public string Priority { get; set; } = "-";
    public decimal? AmountPaid { get; set; }
    public string ManagerNote { get; set; } = string.Empty;

    public string? AssignedTechnician { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CustomerUserId { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string ApplianceBrand { get; set; } = string.Empty;
    public string ApplianceModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? PreferredServiceDate { get; set; }
    public DateTime? NextVisitDate { get; set; }
    public string IssueCategory { get; set; } = string.Empty;
    public string TechnicianNote { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime? TechnicianActionAt { get; set; }
    public ICollection<ServiceRequestPart> Parts { get; set; } = new List<ServiceRequestPart>();
}
