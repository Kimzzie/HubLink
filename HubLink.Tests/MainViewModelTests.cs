using HubLink.Application;
using HubLink.Desktop.ViewModels;
using HubLink.Domain;

namespace HubLink.Tests;

internal sealed class FakeShipmentService : IShipmentService
{
    public Queue<Task<ShipmentFetchResult>> Responses { get; } = new();
    public int Calls { get; private set; }

    public Task<ShipmentFetchResult> GetShipmentsAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Responses.Dequeue();
    }
}

internal sealed class FakeExporter : ICsvExporter
{
    public IReadOnlyList<Shipment>? Exported { get; private set; }

    public Task<ExportResult> ExportAsync(
        IReadOnlyList<Shipment> shipments, string filePath, CancellationToken cancellationToken = default)
    {
        Exported = shipments;
        return Task.FromResult(new ExportResult(true, "exported"));
    }
}

internal sealed class FakePicker(string? path) : IExportFilePicker
{
    public int Calls { get; private set; }

    public Task<string?> PickSavePathAsync(string suggestedFileName)
    {
        Calls++;
        return Task.FromResult(path);
    }
}

public class MainViewModelTests
{
    private static Shipment Make(string id, ShipmentStatus status) =>
        new(id, "A", "B", status, DateTimeOffset.UtcNow);

    private static Task<ShipmentFetchResult> Ok(params Shipment[] shipments) =>
        Task.FromResult(ShipmentFetchResult.Success(shipments));

    private static Task<ShipmentFetchResult> Fail() =>
        Task.FromResult(ShipmentFetchResult.Failure(FetchFailureKind.HttpError, "boom"));

    private static (MainViewModel Vm, FakeShipmentService Svc, FakeExporter Exp, FakePicker Pick) Create()
    {
        var svc = new FakeShipmentService();
        var exp = new FakeExporter();
        var pick = new FakePicker("report.csv");
        return (new MainViewModel(svc, exp, pick, new HubLinkSettings()), svc, exp, pick);
    }

    private static Shipment[] FiveShipments() =>
    [
        Make("1", ShipmentStatus.Pending),
        Make("2", ShipmentStatus.InTransit),
        Make("3", ShipmentStatus.InTransit),
        Make("4", ShipmentStatus.Delivered),
        Make("5", ShipmentStatus.Exception)
    ];

    [Fact]
    public async Task Refresh_Success_ShowsShipmentsAndSummary()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Ok(FiveShipments()));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(5, vm.VisibleShipments.Count);
        Assert.Equal(new ShipmentSummary(5, 1, 2, 1, 1), vm.Summary);
        Assert.False(vm.HasError);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task Filter_ChangesVisibleList_NotSummary_AndMakesNoNewRequest()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Ok(FiveShipments()));
        await vm.RefreshCommand.ExecuteAsync(null);

        vm.SelectedFilter = MainViewModel.StatusFilters.Single(f => f.Status == ShipmentStatus.InTransit);

        Assert.Equal(2, vm.VisibleShipments.Count);
        Assert.All(vm.VisibleShipments, s => Assert.Equal(ShipmentStatus.InTransit, s.Status));
        Assert.Equal(5, vm.Summary!.Total);   // summary still covers ALL shipments
        Assert.Equal(1, svc.Calls);           // no extra HTTP call

        vm.SelectedFilter = MainViewModel.StatusFilters[0];   // "All statuses"
        Assert.Equal(5, vm.VisibleShipments.Count);
    }

    [Fact]
    public async Task EmptyResult_IsNotAnError()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Ok());

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.True(vm.IsEmptyResult);
        Assert.False(vm.HasError);
        Assert.Equal(0, vm.Summary!.Total);
    }

    [Fact]
    public async Task FirstLoadFails_ShowsError_AndNoData()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Fail());

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("boom", vm.ErrorMessage);
        Assert.False(vm.IsShowingPreviousData);
        Assert.Empty(vm.VisibleShipments);
        Assert.Null(vm.Summary);
    }

    [Fact]
    public async Task FailedRefresh_AfterSuccess_KeepsOldDataButLabelsItPrevious()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Ok(Make("1", ShipmentStatus.Pending), Make("2", ShipmentStatus.Delivered)));
        svc.Responses.Enqueue(Fail());

        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.True(vm.IsShowingPreviousData);
        Assert.NotNull(vm.PreviousDataNotice);
        Assert.Equal(2, vm.VisibleShipments.Count);
        Assert.Equal(2, vm.Summary!.Total);
    }

    [Fact]
    public async Task Retry_AfterFailure_Succeeds_AndClearsTheError()
    {
        var (vm, svc, _, _) = Create();
        svc.Responses.Enqueue(Fail());
        svc.Responses.Enqueue(Ok(Make("1", ShipmentStatus.Pending)));

        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);   // retry

        Assert.False(vm.HasError);
        Assert.False(vm.IsShowingPreviousData);
        Assert.Single(vm.VisibleShipments);
    }

    [Fact]
    public async Task Refresh_IsDisabledWhileLoading()
    {
        var (vm, svc, _, _) = Create();
        var pending = new TaskCompletionSource<ShipmentFetchResult>();
        svc.Responses.Enqueue(pending.Task);

        var running = vm.RefreshCommand.ExecuteAsync(null);

        Assert.True(vm.IsLoading);
        Assert.False(vm.RefreshCommand.CanExecute(null));

        pending.SetResult(ShipmentFetchResult.Success([]));
        await running;

        Assert.False(vm.IsLoading);
        Assert.True(vm.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task Export_UsesOnlyTheFilteredShipments()
    {
        var (vm, svc, exp, _) = Create();
        svc.Responses.Enqueue(Ok(FiveShipments()));
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SelectedFilter = MainViewModel.StatusFilters.Single(f => f.Status == ShipmentStatus.InTransit);

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.True(vm.ExportSucceeded);
        Assert.Equal(2, exp.Exported!.Count);
        Assert.All(exp.Exported, s => Assert.Equal(ShipmentStatus.InTransit, s.Status));
    }

    [Fact]
    public async Task Export_WithNothingVisible_ExplainsWhy_AndNeverOpensTheFilePicker()
    {
        var (vm, svc, _, pick) = Create();
        svc.Responses.Enqueue(Ok());
        await vm.RefreshCommand.ExecuteAsync(null);

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.False(vm.ExportSucceeded);
        Assert.Contains("no shipments", vm.ExportMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, pick.Calls);
    }
}