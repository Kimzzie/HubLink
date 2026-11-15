using HubLink.Domain;

namespace HubLink.Tests;

public class ShipmentSummaryTests
{
    private static Shipment Make(string id, ShipmentStatus status) =>
        new(id, "A", "B", status, DateTimeOffset.UtcNow);

    [Fact]
    public void From_CountsEachStatusAndTotal()
    {
        var shipments = new[]
        {
            Make("1", ShipmentStatus.Pending),
            Make("2", ShipmentStatus.InTransit),
            Make("3", ShipmentStatus.InTransit),
            Make("4", ShipmentStatus.Delivered),
            Make("5", ShipmentStatus.Exception)
        };

        var summary = ShipmentSummary.From(shipments);

        Assert.Equal(new ShipmentSummary(5, 1, 2, 1, 1), summary);
    }

    [Fact]
    public void From_EmptyList_GivesAllZeros()
    {
        Assert.Equal(new ShipmentSummary(0, 0, 0, 0, 0), ShipmentSummary.From([]));
    }
}