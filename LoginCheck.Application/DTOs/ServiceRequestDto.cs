namespace LoginCheck.Application.DTOs;

public class ServiceRequestDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Appliance { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? AmountPaid { get; set; }
    public string ManagerNote { get; set; } = string.Empty;
    public string? AssignedTechnician { get; set; }
    public string? AssignedTechnicianPhone { get; set; }
    public DateTime Date { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public int? CustomerUserId { get; set; }
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
    public List<ServiceRequestPartDto> AssignedParts { get; set; } = [];
}

public sealed class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ServiceRequestPartDto
{
    public int Id { get; set; }
    public int InventoryPartId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApplianceType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class CreateServiceRequestDto
{
    [System.ComponentModel.DataAnnotations.StringLength(2000)] public string ManagerNote { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Appliance { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string? AssignedTechnician { get; set; }
    public string ApplianceType { get; set; } = string.Empty;
    public string ApplianceBrand { get; set; } = string.Empty;
    public string ApplianceModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IssueCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
    public DateTime? PreferredServiceDate { get; set; }
    public DateTime? NextVisitDate { get; set; }
    public string Urgency { get; set; } = "Normal";
}
