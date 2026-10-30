namespace LoginCheck.Domain.Entities;

public sealed class ServiceRequestPart
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int InventoryPartId { get; set; }
    public int Quantity { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public InventoryPart InventoryPart { get; set; } = null!;
    public ServiceRequest ServiceRequest { get; set; } = null!;
}
