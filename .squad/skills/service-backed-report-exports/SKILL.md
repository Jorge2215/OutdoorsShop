# Service-backed report exports

## When to use
Use this pattern when an endpoint must return CSV or Excel downloads for existing domain data without introducing storage uploads or background jobs.

## Pattern
1. Query and shape report rows in a service (`IOrderService`, `IInventoryService`) so business filters stay outside controllers.
2. Return flat report DTOs from `OutdoorsShop.Core/DTOs/Reports/`.
3. In the API controller, switch on `format=csv|excel`.
4. Use `CsvHelper` for CSV output and `ClosedXML` for `.xlsx` output.
5. Keep filenames simple and keep HTTP concerns (`File(...)`, content type) inside the controller.

## OutdoorsShop example
- `src/OutdoorsShop.Api/Controllers/ReportsController.cs`
- `src/OutdoorsShop.Core/DTOs/Reports/OrderReportRowDto.cs`
- `src/OutdoorsShop.Core/DTOs/Reports/InventoryReportRowDto.cs`
- `src/OutdoorsShop.Infrastructure/Services/OrderService.cs`
- `src/OutdoorsShop.Infrastructure/Services/InventoryService.cs`

## Gotchas
- Always emit headers even when the result set is empty.
- Keep report DTOs flat; nested collections do not export cleanly to CSV/Excel tables.
- Validate query parameters like date ranges before rendering files.
