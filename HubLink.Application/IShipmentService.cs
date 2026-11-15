namespace HubLink.Application;

public interface IShipmentService
{
    Task<ShipmentFetchResult> GetShipmentsAsync(CancellationToken cancellationToken = default);
}