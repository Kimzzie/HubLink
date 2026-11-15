# HubLink – Regional Logistics Data Exchange (ITSD81 Performance Task 5)

A desktop client (C#, .NET 10, Avalonia UI, MVVM) that retrieves shipment data from the
HubLink REST API, filters and summarizes it, and exports the displayed records to CSV.

## Requirements
- .NET 10 SDK
- The instructor-provided HubLink local service (default address `http://127.0.0.1:5080`)

## Configure
Edit `HubLink.Desktop/appsettings.json`, then restart the app (settings are read at startup).

| Key | Meaning | Normal value |
|---|---|---|
| BaseUrl | Service address | `http://127.0.0.1:5080` |
| BranchCode | Branch to retrieve | `BR-001` |
| EndpointPath | `/api/shipments` normally; `/api/test/shipments` for test modes | `/api/shipments` |
| ExtraQuery | Empty normally. For tests, e.g. `mode=delay` | (empty) |
| TimeoutSeconds | Client timeout in seconds (must be greater than 0) | `5` |

Valid test modes: `empty`, `bad-request`, `server-error`, `invalid-json`, `delay`.
Restore the normal values after testing.

## Run
1. Start the HubLink service (`Start-HubLink.cmd`) and keep its window open.
2. `dotnet restore`
3. `dotnet run --project HubLink.Desktop`

The app loads shipments when the window opens. Use **Refresh** (or **Retry** after an error)
to request them again.

## Use
- **Status** dropdown filters the list without calling the service again.
- The summary always counts all retrieved shipments, not only the filtered ones.
- **Export CSV** saves exactly the shipments currently displayed.
- If a refresh fails after data was loaded, the old rows stay visible with a notice that they
  are previously loaded data.

## Tests
`dotnet test` runs 25 unit tests (summary counts, HTTP/JSON failure mapping, CSV escaping,
ViewModel behavior). They use fake services. Real-service checks are in
`docs/VerificationChecklist.md`.

## Architecture
```
HubLink.Domain          Shipment, ShipmentStatus, ShipmentSummary
HubLink.Application     IShipmentService, ICsvExporter, IExportFilePicker, result types, settings
HubLink.Infrastructure  HttpShipmentService (HttpClient + JSON), CsvExporter
HubLink.Desktop         Avalonia views, MainViewModel, dependency injection setup
HubLink.Tests           xUnit tests
```
Dependencies point inward. Views and ViewModels contain no HTTP or JSON code; the ViewModel
talks only to interfaces. A failed request is returned as a result object (success or a
failure kind plus message), not thrown into the UI.

## Sample output
`docs/sample/shipments_BR-001_20261115_171523.csv` was exported by the application from the
local service (BR-001, all statuses, no manual edits).

## Known limitations
- The UI was verified by manual runs against the local service; there are no automated UI tests.
- Only the controlled test modes and the local service were used. `mode=empty` was not run;
  the empty case was checked with branch `BR-999`.
- Long origin or destination values are trimmed with "..." in the grid. The CSV always
  contains the full value.
- `TimeoutSeconds` set to 0 or a negative value is not validated and would make every request
  fail as a timeout.
- No automatic retry or polling; the user triggers every refresh.
- CSV values beginning with `=`, `+`, `-` or `@` are written unchanged, so a spreadsheet could
  interpret them as formulas. The test data does not contain such values.
- Tested on Windows only.

## Acknowledgements
Parts of the design and code were developed with AI assistance (Claude, Anthropic): the
layered structure, HTTP and JSON handling, CSV export, ViewModel, and test cases.
We reviewed, ran, and tested the result, and we can explain the implementation.