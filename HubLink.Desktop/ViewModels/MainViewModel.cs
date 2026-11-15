using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HubLink.Application;
using HubLink.Domain;

namespace HubLink.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public static IReadOnlyList<StatusFilterOption> StatusFilters { get; } =
    [
        new("All statuses", null),
        new("Pending", ShipmentStatus.Pending),
        new("InTransit", ShipmentStatus.InTransit),
        new("Delivered", ShipmentStatus.Delivered),
        new("Exception", ShipmentStatus.Exception)
    ];

    private readonly IShipmentService _shipmentService;
    private readonly ICsvExporter _csvExporter;
    private readonly IExportFilePicker _filePicker;
    private readonly HubLinkSettings _settings;

    // Everything retrieved. The filter and the summary both start from this list.
    private IReadOnlyList<Shipment> _allShipments = [];

    public MainViewModel(
        IShipmentService shipmentService,
        ICsvExporter csvExporter,
        IExportFilePicker filePicker,
        HubLinkSettings settings)
    {
        _shipmentService = shipmentService;
        _csvExporter = csvExporter;
        _filePicker = filePicker;
        _settings = settings;
    }

    public string BranchCode => _settings.BranchCode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool isLoading;

    // What the grid shows (after filtering).
    [ObservableProperty]
    private IReadOnlyList<Shipment> visibleShipments = [];

    // Counts of ALL retrieved shipments. Null until the first success.
    [ObservableProperty]
    private ShipmentSummary? summary;

    [ObservableProperty]
    private StatusFilterOption selectedFilter = StatusFilters[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => ErrorMessage is not null;

    // True only after a successful response that contained zero shipments.
    [ObservableProperty]
    private bool isEmptyResult;

    // Data exists, but the active filter hides all of it.
    [ObservableProperty]
    private bool noMatchesForFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousDataNotice))]
    private bool isShowingPreviousData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousDataNotice))]
    private DateTimeOffset? lastSuccessfulLoad;

    public string? PreviousDataNotice =>
        IsShowingPreviousData && LastSuccessfulLoad is { } t
            ? $"Showing previously loaded data from {t.ToLocalTime():g}. The last refresh failed, so it was NOT updated."
            : null;

    [ObservableProperty]
    private string? exportMessage;

    [ObservableProperty]
    private bool exportSucceeded;

    partial void OnSelectedFilterChanged(StatusFilterOption value) => ApplyFilter();

    // Refresh and Retry are the same command.
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        ExportMessage = null;
        try
        {
            var result = await _shipmentService.GetShipmentsAsync();

            if (result.IsSuccess)
            {
                _allShipments = result.Shipments;
                Summary = ShipmentSummary.From(_allShipments);
                IsEmptyResult = _allShipments.Count == 0;
                IsShowingPreviousData = false;
                LastSuccessfulLoad = DateTimeOffset.UtcNow;
                ApplyFilter();
            }
            else
            {
                // Never present a failure as a success. Old data may stay, but it is labelled.
                ErrorMessage = result.ErrorMessage;
                IsEmptyResult = false;
                IsShowingPreviousData = _allShipments.Count > 0;
                if (_allShipments.Count == 0)
                    Summary = null;
            }
        }
        finally
        {
            IsLoading = false;   // always runs, so the loading state can't get stuck
        }
    }

    private bool CanRefresh() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        ExportMessage = null;

        // Snapshot: export exactly what is displayed right now (OR-08).
        var rows = VisibleShipments;

        if (rows.Count == 0)
        {
            SetExport(false, "There are no shipments to export. No file was created.");
            return;
        }

        var path = await _filePicker.PickSavePathAsync(BuildSuggestedFileName());
        if (path is null)
            return;   // user cancelled

        var result = await _csvExporter.ExportAsync(rows, path);
        SetExport(result.IsSuccess, result.Message);
    }

    private bool CanExport() => !IsLoading;

    // Call once when the window opens (Stage 7).
    public Task LoadAsync() => RefreshCommand.ExecuteAsync(null);

    private void ApplyFilter()
    {
        var status = SelectedFilter.Status;
        VisibleShipments = status is null
            ? _allShipments
            : _allShipments.Where(s => s.Status == status).ToList();

        NoMatchesForFilter = _allShipments.Count > 0 && VisibleShipments.Count == 0;
    }

    private void SetExport(bool success, string message)
    {
        ExportSucceeded = success;
        ExportMessage = message;
    }

    private string BuildSuggestedFileName() =>
        $"shipments_{_settings.BranchCode}_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.csv";
}