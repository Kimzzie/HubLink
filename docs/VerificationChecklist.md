# HubLink Verification Checklist

Date verified: 2026-11-15. Method: the real application run against the instructor
HubLink local service, with settings changed only in `appsettings.json`.
Unit tests (`dotnet test`) are supporting evidence only.

| ID | Scenario | Status | How verified | Evidence |
|---|---|---|---|---|
| AC-01 | Retrieve shipments for the configured branch | Verified | BR-001 loaded; compared all 5 records field by field with the browser JSON | Includes comma and quote values (HUB-1004) |
| AC-02 | Filter by status | Verified | Selected each status, then returned to All statuses | Pending 1, InTransit 2, Delivered 1, Exception 1; All restores 5 |
| AC-03 | Summary counts | Verified | Compared with API data and watched under each filter | Total 5, Pending 1, InTransit 2, Delivered 1, Exception 1 |
| AC-04 | Refresh without freezing the UI | Verified | `mode=delay`, TimeoutSeconds 15 | Progress bar ~10s; window stayed responsive; Refresh and Export disabled while loading |
| AC-05 | Service failures and recovery | Verified | Timeout (`mode=delay`, 5s), `server-error`, `bad-request`, service stopped, Retry after restart | Messages: no response within 5 seconds / HTTP 500 / HTTP 400 / could not reach service. Service console logged `returned 500` and `returned 400`. Retry loaded data after restart without restarting the app |
| AC-05+ | Old data after failed refresh | Verified | Loaded BR-001, stopped the service, clicked Refresh | Old rows kept with notice that data is previously loaded and was NOT updated |
| AC-06 | Empty and invalid responses | Verified | BR-999 (empty); `mode=invalid-json` | Empty: "no shipments" message, Total 0, no error banner. Invalid: "could not be read as shipment data", no crash, no summary |
| AC-07 | Export with a filter | Verified | Exported with InTransit active; exported All statuses | Filtered file: header + HUB-1001, HUB-1005 only. Full file: `docs/sample/shipments_BR-001_20261115_171523.csv` (395 bytes, header + 5 rows). Line 5 inspected: `HUB-1004,"Butuan, Agusan del Norte","Depot ""North""",Exception,2026-11-07T09:00:00Z` (comma quoted, inner quotes doubled) |
| AC-08 | Export failure | Verified | Exported over a file locked by Excel | Red message "file could not be saved ... being used by another process"; no success message |

## Not verified

- **`mode=empty` test endpoint:** not run. The empty-result case (AC-06) was checked with branch
  `BR-999` on the normal endpoint instead, which also returned an empty shipment list with HTTP 200.
- **Cancelling the save dialog:** not run manually. The ViewModel is designed to treat a cancelled
  dialog as "no export, no message", but this was not checked on screen.
- **Invalid `TimeoutSeconds` (0 or negative):** not tested, and not validated by the application.
- **Other branches or large data sets:** only BR-001 (5 shipments) and BR-999 (empty) were used.
- **Automated UI tests:** none. All screen behavior was checked by manual runs.
- **Platforms other than Windows:** not tested.

## Unit tests (supporting evidence)
`dotnet test`: (PS C:\Users\CLIENT\Documents\HubLink> dotnet test
Restore complete (2.9s)
  HubLink.Domain net10.0 succeeded (0.8s) → HubLink.Domain\bin\Debug\net10.0\HubLink.Domain.dll
  HubLink.Application net10.0 succeeded (1.0s) → HubLink.Application\bin\Debug\net10.0\HubLink.Application.dll
  HubLink.Infrastructure net10.0 succeeded (1.2s) → HubLink.Infrastructure\bin\Debug\net10.0\HubLink.Infrastructure.dll
  HubLink.Desktop net10.0 succeeded (1.2s) → HubLink.Desktop\bin\Debug\net10.0\HubLink.Desktop.dll
  HubLink.Tests net10.0 succeeded (1.8s) → HubLink.Tests\bin\Debug\net10.0\HubLink.Tests.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.15)
[xUnit.net 00:00:01.22]   Discovering: HubLink.Tests
[xUnit.net 00:00:01.30]   Discovered:  HubLink.Tests
[xUnit.net 00:00:01.35]   Starting:    HubLink.Tests
[xUnit.net 00:00:03.53]   Finished:    HubLink.Tests
  HubLink.Tests test net10.0 succeeded (7.0s)

Test summary: total: 25, failed: 0, succeeded: 25, skipped: 0, duration: 6.8s
Build succeeded in 15.8s)