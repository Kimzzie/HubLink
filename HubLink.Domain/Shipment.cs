namespace HubLink.Domain;

public sealed record Shipment(
    string TrackingNumber,
    string Origin,
    string Destination,
    ShipmentStatus Status,
    DateTimeOffset LastUpdatedUtc);