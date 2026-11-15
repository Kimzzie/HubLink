using HubLink.Domain;

namespace HubLink.Desktop.ViewModels;

// Status == null means "All statuses".
public sealed record StatusFilterOption(string Label, ShipmentStatus? Status);