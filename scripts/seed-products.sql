-- =============================================================
-- OutdoorsShop — Product & Inventory Seed Script
-- Target: azure-sql-pampa.database.windows.net / OutdoorsShopDB
-- Date: 2026-05-24
-- Categories: 1=Camping, 2=Trekking, 3=Cycling, 4=Climbing
-- =============================================================

-- Guard: skip if already seeded
IF EXISTS (SELECT 1 FROM Products WHERE IsActive = 1)
BEGIN
    PRINT 'Products already seeded — skipping.';
    RETURN;
END

SET IDENTITY_INSERT Products ON;

INSERT INTO Products (ProductID, Name, CategoryID, Price, Description, ImageUrl, IsActive, DiscountMultiplier)
VALUES
-- Camping (CategoryID = 1)
(1, 'Alpine Base Camp Tent 4P', 1, 149.99,
 '4-person double-wall tent with aluminum poles, waterproof 3000mm fly, and mesh inner for warm-weather ventilation.',
 NULL, 1, 1.0000),

(2, 'TrailRest Mummy Sleeping Bag -10C', 1, 89.99,
 'Synthetic-fill mummy bag rated to -10C. Compression sack included. Hood cinch for cold nights.',
 NULL, 1, 1.0000),

(3, 'Summit Lite Backpacking Stove', 1, 49.99,
 'Ultra-light canister stove (110 g) with piezo igniter. Boils 1 L water in under 3 minutes.',
 NULL, 1, 1.0000),

(4, 'NightTrail 350 Headlamp', 1, 34.99,
 '350-lumen LED headlamp with 3 modes, red night-vision mode, and IPX4 water resistance. 50-hour battery life.',
 NULL, 1, 1.0000),

-- Trekking (CategoryID = 2)
(5, 'Trailblazer Carbon Trekking Poles (pair)', 2, 74.99,
 'Carbon-fiber anti-shock poles with cork grips and quick-lock adjustment. Folds to 60 cm for packing.',
 NULL, 1, 1.0000),

(6, 'Granite Ridge Hiking Boots Mid', 2, 129.99,
 'Full-grain leather waterproof boots with Vibram outsole. Ankle support for technical trails. Sizes 37-47.',
 NULL, 1, 1.0000),

(7, 'HydroFlow 3L Hydration Pack', 2, 59.99,
 '3-litre bladder pack with bite valve, insulated tube sleeve, and extra zip pockets for gear.',
 NULL, 1, 1.0000),

(8, 'TrailNavigator GPS 500', 2, 199.99,
 'Ruggedized handheld GPS with preloaded topographic maps, 20-hour battery, and track recording.',
 NULL, 1, 1.0000),

-- Cycling (CategoryID = 3)
(9, 'VertexMTB Trail Helmet', 3, 69.99,
 'MIPS-equipped mountain bike helmet with 19 vents, adjustable visor, and BOA fit system. CE EN1078.',
 NULL, 1, 1.0000),

(10, 'GripForce Cycling Gloves Full-Finger', 3, 29.99,
 'Full-finger gloves with gel padding, silicone grip pattern, and touchscreen-compatible fingertips.',
 NULL, 1, 1.0000),

(11, 'LumaBolt 1000 Bike Light Set', 3, 44.99,
 'Front 1000-lumen plus rear 100-lumen USB-C rechargeable set. 5 modes, IPX5 waterproof, easy bar mount.',
 NULL, 1, 1.0000),

(12, 'TrailFix Pro Bike Repair Kit', 3, 24.99,
 'All-in-one kit: multi-tool, tyre levers, patches, CO2 inflator, and chain link. Fits in jersey pocket.',
 NULL, 1, 1.0000),

-- Climbing (CategoryID = 4)
(13, 'Ascent Pro Climbing Harness', 4, 89.99,
 'Sport-climbing harness with four gear loops, padded waist and leg loops. CE EN12277 Type C certified.',
 NULL, 1, 1.0000),

(14, 'Summit Chalk Bag with Belt', 4, 19.99,
 'Drawstring chalk bag with stiff rim, brush holder, and adjustable belt. Available in 6 colours.',
 NULL, 1, 1.0000),

(15, 'VértexEdge Rock Climbing Shoes', 4, 109.99,
 'Neutral all-around shoe with sticky Vibram XS Grip2 rubber. Great for sport routes and bouldering.',
 NULL, 1, 1.0000),

(16, 'IronLink Carabiner Set 6-pack', 4, 39.99,
 'Lightweight aluminium screwgate carabiners, 24 kN rated. Anodised colours for easy rack identification.',
 NULL, 1, 1.0000);

SET IDENTITY_INSERT Products OFF;

-- Inventory rows (actual columns: QuantityAvailable, ReorderThreshold)
INSERT INTO Inventory (ProductID, QuantityAvailable, ReorderThreshold, LastUpdated)
VALUES
(1,  30, 5, GETDATE()),
(2,  45, 5, GETDATE()),
(3,  50, 5, GETDATE()),
(4,  40, 5, GETDATE()),
(5,  25, 5, GETDATE()),
(6,  20, 5, GETDATE()),
(7,  35, 5, GETDATE()),
(8,  15, 5, GETDATE()),
(9,  30, 5, GETDATE()),
(10, 50, 5, GETDATE()),
(11, 40, 5, GETDATE()),
(12, 45, 5, GETDATE()),
(13, 25, 5, GETDATE()),
(14, 50, 5, GETDATE()),
(15, 20, 5, GETDATE()),
(16, 35, 5, GETDATE());

PRINT 'Seed complete -- 16 products and 16 inventory rows inserted.';
GO
