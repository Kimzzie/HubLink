using HubLink.Domain;

namespace HubLink.Application;

public sealed record ShipmentFetchResult
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<Shipment> Shipments { get; private init; } = [];
    public FetchFailureKind? FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }

    // An empty list is still a success.
    public static ShipmentFetchResult Success(IReadOnlyList<Shipment> shipments) =>
        new() { IsSuccess = true, Shipments = shipments };

    public static ShipmentFetchResult Failure(FetchFailureKind kind, string message) =>
        new() { IsSuccess = false, FailureKind = kind, ErrorMessage = message };
}