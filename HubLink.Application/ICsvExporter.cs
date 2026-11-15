using HubLink.Domain;

namespace HubLink.Application;

public interface ICsvExporter
{
    Task<ExportResult> ExportAsync(
        IReadOnlyList<Shipment> shipments,
        string filePath,
        CancellationToken cancellationToken = default);
}