using System.Net;
using System.Text;
using HubLink.Application;
using HubLink.Domain;
using HubLink.Infrastructure;

namespace HubLink.Tests;

internal sealed class StubHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request, cancellationToken);
}

public class HttpShipmentServiceTests
{
    private static HttpShipmentService Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler,
        int timeoutSeconds = 5) =>
        new(new HttpClient(new StubHandler(handler)),
            new HubLinkSettings { TimeoutSeconds = timeoutSeconds });

    private static Task<HttpResponseMessage> Reply(HttpStatusCode code, string body) =>
        Task.FromResult(new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

    [Fact]
    public async Task Success_ParsesShipments_IncludingCommasAndQuotes()
    {
        var json = """
        { "branchCode": "BR-001", "shipments": [
          { "trackingNumber": "HUB-1", "branchCode": "BR-001",
            "origin": "Cagayan de Oro, \"Hub\"", "destination": "Valencia",
            "status": "InTransit", "lastUpdatedUtc": "2026-11-10T08:30:00Z" } ] }
        """;
        var result = await Create((_, _) => Reply(HttpStatusCode.OK, json)).GetShipmentsAsync();

        Assert.True(result.IsSuccess);
        var s = Assert.Single(result.Shipments);
        Assert.Equal("Cagayan de Oro, \"Hub\"", s.Origin);
        Assert.Equal(ShipmentStatus.InTransit, s.Status);
    }

    [Fact]
    public async Task EmptyList_IsSuccess_NotFailure()
    {
        var result = await Create((_, _) =>
            Reply(HttpStatusCode.OK, """{ "branchCode": "BR-999", "shipments": [] }"""))
            .GetShipmentsAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Shipments);
    }

    [Fact]
    public async Task Http500_IsHttpError_WithServerMessage()
    {
        var result = await Create((_, _) =>
            Reply(HttpStatusCode.InternalServerError, """{"error":"Simulated server error."}"""))
            .GetShipmentsAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(FetchFailureKind.HttpError, result.FailureKind);
        Assert.Contains("500", result.ErrorMessage);
        Assert.Contains("Simulated server error.", result.ErrorMessage);
    }

    [Fact]
    public async Task IncompleteJson_IsInvalidResponse()
    {
        var result = await Create((_, _) =>
            Reply(HttpStatusCode.OK, """{ "branchCode": "BR-001", "shipments": [ { "trackingNum"""))
            .GetShipmentsAsync();

        Assert.Equal(FetchFailureKind.InvalidResponse, result.FailureKind);
    }

    [Fact]
    public async Task UnknownStatus_IsInvalidResponse()
    {
        var json = """
        { "shipments": [ { "trackingNumber": "X", "origin": "A", "destination": "B",
          "status": "Lost", "lastUpdatedUtc": "2026-11-10T08:30:00Z" } ] }
        """;
        var result = await Create((_, _) => Reply(HttpStatusCode.OK, json)).GetShipmentsAsync();

        Assert.Equal(FetchFailureKind.InvalidResponse, result.FailureKind);
    }

    [Fact]
    public async Task ConnectionFailure_IsServiceUnreachable()
    {
        var result = await Create((_, _) => throw new HttpRequestException("refused"))
            .GetShipmentsAsync();

        Assert.Equal(FetchFailureKind.ServiceUnreachable, result.FailureKind);
    }

    [Fact]
    public async Task SlowService_IsTimeout()
    {
        var service = Create(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);   // never answers; cancelled by our timer
            return new HttpResponseMessage();
        }, timeoutSeconds: 1);

        var result = await service.GetShipmentsAsync();

        Assert.Equal(FetchFailureKind.Timeout, result.FailureKind);
    }

    [Fact]
    public async Task Request_UsesConfiguredBranchAndPath()
    {
        Uri? seen = null;
        var service = Create((req, _) =>
        {
            seen = req.RequestUri;
            return Reply(HttpStatusCode.OK, """{ "shipments": [] }""");
        });

        await service.GetShipmentsAsync();

        Assert.Equal("/api/shipments", seen!.AbsolutePath);
        Assert.Contains("branchCode=BR-001", seen.Query);
    }
}