using HubLink.Domain;
using HubLink.Infrastructure;

namespace HubLink.Tests;

public class CsvExporterTests
{
    private static Shipment Make(string id, string origin = "A", string destination = "B",
        ShipmentStatus status = ShipmentStatus.Pending) =>
        new(id, origin, destination, status,
            new DateTimeOffset(2026, 11, 10, 8, 30, 0, TimeSpan.Zero));

    [Fact]
    public void BuildCsv_HasHeaderInRequiredOrder_AndOneRowPerShipment()
    {
        var csv = CsvExporter.BuildCsv([Make("HUB-1")]);

        Assert.Equal(
            "TrackingNumber,Origin,Destination,Status,LastUpdatedUtc\r\n" +
            "HUB-1,A,B,Pending,2026-11-10T08:30:00Z\r\n",
            csv);
    }

    [Fact]
    public void BuildCsv_QuotesFieldsWithCommasAndDoublesQuotes()
    {
        var csv = CsvExporter.BuildCsv(
            [Make("HUB-1", origin: "Cagayan de Oro, \"Hub\"", status: ShipmentStatus.InTransit)]);

        Assert.Equal(
            "TrackingNumber,Origin,Destination,Status,LastUpdatedUtc\r\n" +
            "HUB-1,\"Cagayan de Oro, \"\"Hub\"\"\",B,InTransit,2026-11-10T08:30:00Z\r\n",
            csv);
    }

    [Fact]
    public void BuildCsv_QuotesFieldsContainingLineBreaks()
    {
        var csv = CsvExporter.BuildCsv([Make("HUB-1", destination: "Line1\nLine2")]);

        Assert.Contains("\"Line1\nLine2\"", csv);
    }

    [Fact]
    public async Task Export_EmptyList_ReturnsFailure_AndCreatesNoFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");

        var result = await new CsvExporter().ExportAsync([], path);

        Assert.False(result.IsSuccess);
        Assert.Contains("no shipments", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Export_WritesHeaderPlusExactlyTheGivenRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        try
        {
            var result = await new CsvExporter().ExportAsync([Make("HUB-1"), Make("HUB-2")], path);

            Assert.True(result.IsSuccess);
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);   // header + 2 rows
            Assert.Equal("TrackingNumber,Origin,Destination,Status,LastUpdatedUtc", lines[0]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Export_ToMissingFolder_ReturnsFailure_NotSuccess()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "report.csv");

        var result = await new CsvExporter().ExportAsync([Make("HUB-1")], path);

        Assert.False(result.IsSuccess);
        Assert.False(File.Exists(path));
    }
}