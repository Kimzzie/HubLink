namespace HubLink.Application;

public sealed class HubLinkSettings
{
    public const string SectionName = "HubLink";

    public string BaseUrl { get; set; } = "http://127.0.0.1:5080";
    public string BranchCode { get; set; } = "BR-001";

    // Normal: /api/shipments   |   Testing: /api/test/shipments
    public string EndpointPath { get; set; } = "/api/shipments";

    // Empty normally. For tests, e.g. "mode=delay"
    public string ExtraQuery { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 5;
}