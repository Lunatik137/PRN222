-- ==========================================================
-- Script for Review - Dang
-- ==========================================================
ALTER TABLE Review 
ADD status NVARCHAR(20) NOT NULL DEFAULT 'Pending';
GO

DECLARE @Buyer1 INT, @Buyer2 INT, @Buyer3 INT;
DECLARE @Seller1 INT, @Seller2 INT, @Seller3 INT;
DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT, @P6 INT;

-- Insert Buyers 
INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('buyer_alice', 'alice@example.com', '123456', 'Buyer', 1, 0, GETDATE(), 'LOW', 10);
SET @Buyer1 = SCOPE_IDENTITY();

INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('buyer_bob', 'bob@example.com', '123456', 'Buyer', 1, 0, GETDATE(), 'MEDIUM', 50);
SET @Buyer2 = SCOPE_IDENTITY();

INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('buyer_charlie', 'charlie@scam.com', '123456', 'Buyer', 1, 0, GETDATE(), 'HIGH', 89);
SET @Buyer3 = SCOPE_IDENTITY();

-- Insert Sellers
INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('seller_dave', 'dave@shop.com', '123456', 'Seller', 1, 0, GETDATE(), 'LOW', 5);
SET @Seller1 = SCOPE_IDENTITY();

INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('seller_eve', 'eve@hitech.com', '123456', 'Seller', 1, 0, GETDATE(), 'MEDIUM', 55);
SET @Seller2 = SCOPE_IDENTITY();

INSERT INTO [User] (username, email, password, role, isApproved, isLocked, createdAt, RiskLevel, RiskScore) 
VALUES 
('seller_frank', 'frank@fakegoods.com', '123456', 'Seller', 1, 0, GETDATE(), 'HIGH', 95);
SET @Seller3 = SCOPE_IDENTITY();

-- Insert Products
INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller1, 'Logitech G Pro X Superlight', 'Great condition', 120.00, 'Active');
SET @P1 = SCOPE_IDENTITY();

INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller1, 'Keychron Q1 Pro', 'Almost new', 150.00, 'Active');
SET @P2 = SCOPE_IDENTITY();

INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller2, 'iPhone 15 Pro Max Titanium', 'Sealed box', 1199.99, 'Active');
SET @P3 = SCOPE_IDENTITY();

INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller2, 'MacBook Pro M3 Max', 'With warranty', 2999.00, 'Active');
SET @P4 = SCOPE_IDENTITY();

INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller3, 'AirPods Pro 2 Fake', 'Cheap knockoff', 20.00, 'Active');
SET @P5 = SCOPE_IDENTITY();

INSERT INTO Product (sellerId, title, description, price, status) VALUES (@Seller3, 'Rolex Submariner Replica', 'Nice watch', 50.00, 'Active');
SET @P6 = SCOPE_IDENTITY();

-- Insert Reviews 
-- Product 1 Reviews (3)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P1, @Buyer1, 5, 'Great mouse, very light.', 'Approved', GETDATE()),
(@P1, @Buyer2, 4, 'Good but expensive.', 'Approved', GETDATE()),
(@P1, @Buyer3, 1, 'Scam! Never arrived.', 'Pending', GETDATE());

-- Product 2 Reviews (4)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P2, @Buyer1, 5, 'Amazing keyboard.', 'Approved', DATEADD(DAY, -1, GETDATE())),
(@P2, @Buyer2, 5, 'Thocky sound, love it.', 'Approved', DATEADD(DAY, -2, GETDATE())),
(@P2, @Buyer3, 2, 'Keys are too heavy for me.', 'Pending', GETDATE()),
(@P2, @Buyer1, 4, 'Solid build quality.', 'Hidden', DATEADD(DAY, -5, GETDATE()));

-- Product 3 Reviews (4)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P3, @Buyer1, 5, 'Perfect phone, arrived fast.', 'Approved', GETDATE()),
(@P3, @Buyer2, 1, 'SCAM!!! This is fake iPhone! Contact me at myemail@scam.com for refund!', 'Pending', GETDATE()),
(@P3, @Buyer3, 5, 'I love this phone.', 'Approved', GETDATE()),
(@P3, @Buyer1, 3, 'Battery drains quickly.', 'Hidden', GETDATE());

-- Product 4 Reviews (3)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P4, @Buyer2, 5, 'A beast of a machine.', 'Approved', GETDATE()),
(@P4, @Buyer3, 5, 'Screen is wonderful.', 'Pending', GETDATE()),
(@P4, @Buyer1, 1, 'Seller sent a brick.', 'Rejected', GETDATE());

-- Product 5 Reviews (3)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P5, @Buyer2, 1, 'Terrible audio quality.', 'Approved', GETDATE()),
(@P5, @Buyer3, 1, 'Broke in 2 days.', 'Approved', GETDATE()),
(@P5, @Buyer1, 1, 'Pls refund me.', 'Pending', GETDATE());

-- Product 6 Reviews (3)
INSERT INTO Review (productId, reviewerId, rating, comment, status, createdAt) VALUES 
(@P6, @Buyer1, 2, 'It looks fake.', 'Approved', GETDATE()),
(@P6, @Buyer2, 1, 'Stop selling this garbage.', 'Hidden', GETDATE()),
(@P6, @Buyer3, 5, 'Good for the price tbh.', 'Pending', GETDATE());

-- Insert Feedback Summaries for new Sellers
INSERT INTO Feedback (sellerId, averageRating, totalReviews, positiveRate)
SELECT id, 4.5, 7, 85.0 FROM [User] WHERE username = 'seller_dave';
INSERT INTO Feedback (sellerId, averageRating, totalReviews, positiveRate)
SELECT id, 3.8, 8, 60.0 FROM [User] WHERE username = 'seller_eve';
INSERT INTO Feedback (sellerId, averageRating, totalReviews, positiveRate)
SELECT id, 1.2, 5, 10.0 FROM [User] WHERE username = 'seller_frank';

PRINT 'Successfully!';
