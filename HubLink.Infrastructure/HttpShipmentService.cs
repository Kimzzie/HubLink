using System.Text.Json;
using System.Text.Json.Serialization;
using HubLink.Application;
using HubLink.Domain;

namespace HubLink.Infrastructure;

public sealed class HttpShipmentService : IShipmentService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)               // camelCase, case-insensitive
        {
            RespectNullableAnnotations = true,        // null for a non-nullable field = invalid
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
        };

    private readonly HttpClient _http;
    private readonly HubLinkSettings _settings;

    public HttpShipmentService(HttpClient http, HubLinkSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<ShipmentFetchResult> GetShipmentsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(BuildUrl(), UriKind.Absolute, out var uri))
            return ShipmentFetchResult.Failure(FetchFailureKind.ServiceUnreachable,
                "The service address in the settings is not a valid URL.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            using var response = await _http.GetAsync(
                uri, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)          // 400, 500... do NOT throw by themselves
                return await HttpFailureAsync(response, timeoutCts.Token);

            return await ReadShipmentsAsync(response, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Our timer fired, not the user. (User cancellation is rethrown automatically.)
            return ShipmentFetchResult.Failure(FetchFailureKind.Timeout,
                $"The service did not respond within {_settings.TimeoutSeconds} seconds.");
        }
        catch (HttpRequestException)
        {
            return ShipmentFetchResult.Failure(FetchFailureKind.ServiceUnreachable,
                "Could not reach the shipment service. Check that it is running and the address is correct.");
        }
    }

    private string BuildUrl()
    {
        var query = $"branchCode={Uri.EscapeDataString(_settings.BranchCode)}";
        if (!string.IsNullOrWhiteSpace(_settings.ExtraQuery))
            query += "&" + _settings.ExtraQuery.TrimStart('&', '?');

        return $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.EndpointPath.TrimStart('/')}?{query}";
    }

    private static async Task<ShipmentFetchResult> ReadShipmentsAsync(
        HttpResponseMessage response, CancellationToken token)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(token);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            // Status was 200 but the body was cut off mid-transfer.
            return ShipmentFetchResult.Failure(FetchFailureKind.InvalidResponse,
                "The service response was incomplete.");
        }

        ShipmentListDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ShipmentListDto>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return ShipmentFetchResult.Failure(FetchFailureKind.InvalidResponse,
                "The service response could not be read as shipment data.");
        }

        if (dto is null)
            return ShipmentFetchResult.Failure(FetchFailureKind.InvalidResponse,
                "The service response was empty or not shipment data.");

        // An empty list is a valid SUCCESS (OR-07).
        var shipments = dto.Shipments
            .Select(s => new Shipment(s.TrackingNumber, s.Origin, s.Destination,
                                      s.Status, s.LastUpdatedUtc))
            .ToList();

        return ShipmentFetchResult.Success(shipments);
    }

    private static async Task<ShipmentFetchResult> HttpFailureAsync(
        HttpResponseMessage response, CancellationToken token)
    {
        var message = $"The shipment service returned an error (HTTP {(int)response.StatusCode}).";

        var detail = await TryReadErrorTextAsync(response, token);
        if (detail is not null)
            message += $" {detail}";

        return ShipmentFetchResult.Failure(FetchFailureKind.HttpError, message);
    }

    // The API sends {"error":"..."} on failures. Use it if present, ignore otherwise.
    private static async Task<string?> TryReadErrorTextAsync(
        HttpResponseMessage response, CancellationToken token)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                   && doc.RootElement.TryGetProperty("error", out var e)
                   && e.ValueKind == JsonValueKind.String
                ? e.GetString()
                : null;
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException or IOException)
        {
            return null;
        }
    }
}