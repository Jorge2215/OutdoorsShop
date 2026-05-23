# Cinnamon backend decisions inbox

- **Timestamp:** 2026-05-23T14:02:03.844-03:00
- **Scope:** Customers, Orders, Inventory, and Reports endpoints for the .NET 10 Web API.

## Proposed team-relevant decisions

1. **Protected ownership rules live in services, not controllers.**
   - `CustomersController` and `OrdersController` pass JWT context into services.
   - `CustomerService` and `OrderService` decide whether a customer can read or mutate a resource.
   - This keeps authorization-by-ownership reusable if Functions, jobs, or future endpoints need the same rule.

2. **Paged responses are now the standard for admin list endpoints.**
   - `PagedResult<T>` was introduced in `src/OutdoorsShop.Core/DTOs/Common/PagedResult.cs`.
   - Customers, Orders, and Inventory list endpoints now share `pageNumber` / `pageSize` semantics.
   - Recommend frontend and tests treat list payloads as `{ items, pageNumber, pageSize, totalCount, totalPages }`.

3. **Order creation is server-validated against current catalog price and stock.**
   - The API accepts `UnitPrice`, but `OrderService` rejects mismatches against the active product price.
   - Inventory decrement and order persistence happen inside one EF Core transaction.
   - This should remain the contract until a dedicated payment/cart service exists.

4. **Report exports should keep data shaping in services and file rendering in API.**
   - Services return report row DTOs.
   - `ReportsController` owns CSV/Excel formatting with `CsvHelper` and `ClosedXML`.
   - This avoids pushing transport-specific libraries into repository code.
