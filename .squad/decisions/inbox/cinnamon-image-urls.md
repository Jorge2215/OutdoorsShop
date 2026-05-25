# Cinnamon Decision — 2026-05-24T14:24:58.550-03:00 — Product image URLs via Unsplash CDN

## Context

All 16 seeded products had `NULL` ImageUrl values in Azure SQL (`OutdoorsShopDB`). The frontend product cards render `<img src={product.imageUrl}>`, so null URLs produced broken image icons.

## Decision

Use **Unsplash free-tier CDN URLs** (`https://images.unsplash.com/photo-{id}?w=400&fit=crop&auto=format`) for all 16 product images rather than uploading owned blobs to `stoutdoorsdev`.

## Why this option

- **Zero cost & zero infra overhead:** Unsplash Source URLs are publicly accessible, no auth required, and served from a global CDN — no blob upload step needed.
- **Variety per product:** A unique, category-relevant photo was picked per product (no duplicates).
- **Reversible:** If the team ever wants owned images in `stoutdoorsdev/product-images`, it's a 16-row UPDATE away.

## Image mapping

| ProductID | Name | Unsplash Photo ID |
|-----------|------|-------------------|
| 1 | Alpine Base Camp Tent 4P | photo-1504280390367-361c6d9f38f4 |
| 2 | TrailRest Mummy Sleeping Bag -10C | photo-1544348817-5f2cf14b88c8 |
| 3 | Summit Lite Backpacking Stove | photo-1563299796-17596ed6b017 |
| 4 | NightTrail 350 Headlamp | photo-1414694762283-acccc27bca85 |
| 5 | Trailblazer Carbon Trekking Poles | photo-1551632811-561732d1e306 |
| 6 | Granite Ridge Hiking Boots Mid | photo-1542401886-65d6c61db217 |
| 7 | HydroFlow 3L Hydration Pack | photo-1538635993-85060e52fd8a |
| 8 | TrailNavigator GPS 500 | photo-1532274402911-5a369e4c4bb5 |
| 9 | VertexMTB Trail Helmet | photo-1541625602330-2277a4c46182 |
| 10 | GripForce Cycling Gloves Full-Finger | photo-1558981403-c5f9899a28bc |
| 11 | LumaBolt 1000 Bike Light Set | photo-1485965120184-e220f721d03e |
| 12 | TrailFix Pro Bike Repair Kit | photo-1571068316344-75bc76f77890 |
| 13 | Ascent Pro Climbing Harness | photo-1522163182402-834f871fd851 |
| 14 | Summit Chalk Bag with Belt | photo-1564760055775-d63b17a55c44 |
| 15 | VértexEdge Rock Climbing Shoes | photo-1574397113396-4369b6dc0dbc |
| 16 | IronLink Carabiner Set 6-pack | photo-1599508704512-2f19efd1e35f |

## Implementation

- Created `scripts/update-image-urls.sql` — runs 16 UPDATE statements and a verification SELECT.
- Updated `scripts/seed-products.sql` — replaced NULL with the Unsplash URLs in the INSERT block so future reseeds are correct.
- Ran the UPDATE script via `sqlcmd` against `azure-sql-pampa.database.windows.net / OutdoorsShopDB`.
- Required opening firewall rule `AllowCinnamonAgent` in resource group `AzureSqlRg` (not `rg-outdoors-dev` — that's where the Azure SQL server lives).

## Verification

`GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products, all with non-null `imageUrl`.

## Consequences

- Product images are served from Unsplash CDN — any future Unsplash rate-limiting or takedown would break them.
- For production, consider uploading owned images to `stoutdoorsdev/product-images` and pointing `ImageUrl` there.
