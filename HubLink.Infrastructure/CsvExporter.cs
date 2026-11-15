using System.Globalization;
using System.Text;
using HubLink.Application;
using HubLink.Domain;

namespace HubLink.Infrastructure;

public sealed class CsvExporter : ICsvExporter
{
    private const string Header = "TrackingNumber,Origin,Destination,Status,LastUpdatedUtc";
    private const string NewLine = "\r\n";
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";
    private static readonly char[] SpecialChars = [',', '"', '\r', '\n'];
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public async Task<ExportResult> ExportAsync(
        IReadOnlyList<Shipment> shipments,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (shipments.Count == 0)
            return new ExportResult(false,
                "There are no shipments to export. No file was created.");

        if (string.IsNullOrWhiteSpace(filePath))
            return new ExportResult(false, "No file location was chosen. No file was created.");

        var content = BuildCsv(shipments);   // format first, so a formatting problem can't leave half a file

        try
        {
            await File.WriteAllTextAsync(filePath, content, Utf8NoBom, cancellationToken);
            return new ExportResult(true, $"Exported {shipments.Count} shipment(s) to {filePath}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                      or ArgumentException or NotSupportedException)
        {
            return new ExportResult(false, $"The file could not be saved. {ex.Message}");
        }
    }

    // Pure function: no file access, easy to test.
    public static string BuildCsv(IEnumerable<Shipment> shipments)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append(NewLine);

        foreach (var s in shipments)
        {
            sb.Append(Escape(s.TrackingNumber)).Append(',')
              .Append(Escape(s.Origin)).Append(',')
              .Append(Escape(s.Destination)).Append(',')
              .Append(Escape(s.Status.ToString())).Append(',')
              .Append(Escape(s.LastUpdatedUtc.UtcDateTime
                              .ToString(TimestampFormat, CultureInfo.InvariantCulture)))
              .Append(NewLine);
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny(SpecialChars) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}