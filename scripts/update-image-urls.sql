-- Update ImageUrl for all 16 products with Unsplash images
-- Camping (CategoryID = 1)
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=400&fit=crop&auto=format' WHERE ProductID = 1;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1544348817-5f2cf14b88c8?w=400&fit=crop&auto=format' WHERE ProductID = 2;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1563299796-17596ed6b017?w=400&fit=crop&auto=format' WHERE ProductID = 3;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1414694762283-acccc27bca85?w=400&fit=crop&auto=format' WHERE ProductID = 4;
-- Trekking (CategoryID = 2)
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1551632811-561732d1e306?w=400&fit=crop&auto=format' WHERE ProductID = 5;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1542401886-65d6c61db217?w=400&fit=crop&auto=format' WHERE ProductID = 6;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1538635993-85060e52fd8a?w=400&fit=crop&auto=format' WHERE ProductID = 7;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1532274402911-5a369e4c4bb5?w=400&fit=crop&auto=format' WHERE ProductID = 8;
-- Cycling (CategoryID = 3)
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1541625602330-2277a4c46182?w=400&fit=crop&auto=format' WHERE ProductID = 9;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=400&fit=crop&auto=format' WHERE ProductID = 10;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=400&fit=crop&auto=format' WHERE ProductID = 11;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1571068316344-75bc76f77890?w=400&fit=crop&auto=format' WHERE ProductID = 12;
-- Climbing (CategoryID = 4)
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1522163182402-834f871fd851?w=400&fit=crop&auto=format' WHERE ProductID = 13;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1564760055775-d63b17a55c44?w=400&fit=crop&auto=format' WHERE ProductID = 14;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1574397113396-4369b6dc0dbc?w=400&fit=crop&auto=format' WHERE ProductID = 15;
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1599508704512-2f19efd1e35f?w=400&fit=crop&auto=format' WHERE ProductID = 16;
SELECT ProductID, Name, ImageUrl FROM Products ORDER BY ProductID;
