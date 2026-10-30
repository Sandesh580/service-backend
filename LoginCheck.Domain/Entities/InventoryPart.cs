namespace LoginCheck.Domain.Entities;

public sealed class InventoryPart
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApplianceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal UnitPrice { get; set; }
    public ICollection<ServiceRequestPart> RequestParts { get; set; } = new List<ServiceRequestPart>();
}
