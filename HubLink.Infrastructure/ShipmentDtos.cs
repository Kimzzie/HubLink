namespace HubLink.Infrastructure;

// Shape of the JSON only. Kept internal so the API format never leaks into Domain.
internal sealed class ShipmentListDto
{
    public required List<ShipmentDto> Shipments { get; init; }
}

internal sealed class ShipmentDto
{
    public required string TrackingNumber { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required HubLink.Domain.ShipmentStatus Status { get; init; }
    public required DateTimeOffset LastUpdatedUtc { get; init; }
}