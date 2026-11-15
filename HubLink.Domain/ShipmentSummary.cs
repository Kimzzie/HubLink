namespace HubLink.Domain;

public sealed record ShipmentSummary(
    int Total, int Pending, int InTransit, int Delivered, int Exception)
{
    public static ShipmentSummary From(IEnumerable<Shipment> shipments)
    {
        var list = shipments.ToList();
        return new ShipmentSummary(
            list.Count,
            list.Count(s => s.Status == ShipmentStatus.Pending),
            list.Count(s => s.Status == ShipmentStatus.InTransit),
            list.Count(s => s.Status == ShipmentStatus.Delivered),
            list.Count(s => s.Status == ShipmentStatus.Exception));
    }
}