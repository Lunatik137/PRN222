USE CloneEbayDB;
GO

BEGIN TRANSACTION;
BEGIN TRY
/* =====================================================
1. ADMIN + USER (PHÂN QUYỀN RÕ RÀNG)
===================================================== */
INSERT INTO [User]
(username, email, password, role, isApproved, isLocked, isTwoFactorEnabled, createdAt, registrationIP)
VALUES
-- Admin
('superadmin', 'superadmin@cloneebay.com', 'hashed_pw', 'SuperAdmin', 1, 0, 1, DATEADD(MONTH,-6,GETDATE()), '10.0.0.1'),
('monitor01',  'monitor@cloneebay.com',    'hashed_pw', 'Monitor',    1, 0, 1, DATEADD(MONTH,-5,GETDATE()), '10.0.0.2'),
('support01',  'support@cloneebay.com',    'hashed_pw', 'Support',    1, 0, 0, DATEADD(MONTH,-4,GETDATE()), '10.0.0.3'),

-- Seller & Buyer
('seller_bad', 'seller_bad@cloneebay.com', 'hashed_pw', 'Seller', 1, 0, 0, DATEADD(MONTH,-3,GETDATE()), '113.22.1.10'),
('seller_good','seller_good@cloneebay.com','hashed_pw', 'Seller', 1, 0, 0, DATEADD(MONTH,-2,GETDATE()), '113.22.1.11'),
('buyer01',    'buyer01@cloneebay.com',    'hashed_pw', 'Buyer',  1, 0, 0, DATEADD(DAY,-10,GETDATE()), '118.70.1.5'),
('buyer02',    'buyer02@cloneebay.com',    'hashed_pw', 'Buyer',  0, 0, 0, DATEADD(DAY,-2,GETDATE()),  '118.70.1.6');

/* =====================================================
2. CATEGORY
===================================================== */
INSERT INTO Category (name)
VALUES (N'Electronics'), (N'Fashion'), (N'Collectibles');

/* =====================================================
3. STORE + PRODUCT (CÓ SẢN PHẨM VI PHẠM)
===================================================== */
INSERT INTO Store (sellerId, storeName, description, storeLevel)
VALUES
(4, N'Store Vi Phạm', N'Shop nhiều report', 1),
(5, N'Store Uy Tín',  N'Shop chính hãng',   2);

INSERT INTO Product
(title, description, price, categoryId, sellerId, isAuction, status)
VALUES
(N'Fake iPhone', N'Nghi hàng giả', 200, 1, 4, 0, N'REPORTED'),
(N'Original iPhone', N'Chính hãng', 1200,1, 5, 0, N'ACTIVE'),
(N'Rare Coin', N'Đấu giá', 500, 3, 5, 1, N'AUCTION');

/* =====================================================
4. INVENTORY
===================================================== */
INSERT INTO Inventory (productId, quantity, lastUpdated)
VALUES
(1, 20, GETDATE()),
(2, 5,  GETDATE()),
(3, 1,  GETDATE());

/* =====================================================
5. ADDRESS + ORDER + PAYMENT (DỮ LIỆU THEO THỜI GIAN)
===================================================== */
INSERT INTO Address
(userId, fullName, phone, street, city, state, country, isDefault)
VALUES
(6, N'Buyer One', '090000001', N'1 Le Loi', N'HCM', N'Q1', N'VN', 1);

INSERT INTO OrderTable
(buyerId, addressId, orderDate, totalPrice, status)
VALUES
(6, 1, DATEADD(DAY,-7,GETDATE()), 1200, N'PAID'),
(6, 1, DATEADD(MONTH,-1,GETDATE()), 500, N'PAID'),
(6, 1, DATEADD(MONTH,-3,GETDATE()), 200, N'REFUNDED');

INSERT INTO Payment
(orderId, userId, amount, method, status, paidAt)
VALUES
(1, 6, 1200, 'CARD', 'SUCCESS', DATEADD(DAY,-7,GETDATE())),
(2, 6, 500,  'CARD', 'SUCCESS', DATEADD(MONTH,-1,GETDATE())),
(3, 6, 200,  'CARD', 'REFUNDED',DATEADD(MONTH,-3,GETDATE()));

/* =====================================================
6. REVIEW + FEEDBACK (GIÁM SÁT ĐÁNH GIÁ)
===================================================== */
INSERT INTO Review
(productId, reviewerId, rating, comment, createdAt)
VALUES
(1, 6, 1, N'Hàng giả', GETDATE()),
(2, 6, 5, N'Rất tốt', GETDATE());

INSERT INTO Feedback
(sellerId, averageRating, totalReviews, positiveRate)
VALUES
(4, 1.5, 10, 10),
(5, 4.8, 120, 96);

/* =====================================================
7. DISPUTE + RETURN REQUEST (ADMIN XỬ LÝ)
===================================================== */
INSERT INTO Dispute
(orderId, raisedBy, description, status)
VALUES
(1, 6, N'Nghi ngờ hàng giả', 'PENDING');

INSERT INTO ReturnRequest
(orderId, userId, reason, status, createdAt)
VALUES
(1, 6, N'Hoàn tiền do vi phạm', 'PENDING', GETDATE());

/* =====================================================
8. RISK ASSESSMENT (SECURITY ADMIN)
===================================================== */
INSERT INTO RiskAssessment
(UserId, RiskScore, RiskLevel, RecommendedAction, Reason,
 RapidRegistrations, DisposableEmail)
VALUES
(4, 90, 'HIGH', 'LOCK', N'Nhiều report + dispute', 1, 1);

/* =====================================================
9. MESSAGE (ADMIN GỬI CẢNH BÁO)
===================================================== */
INSERT INTO Message
(senderId, receiverId, content, [timestamp])
VALUES
(1, 4, N'Tài khoản bạn đang bị điều tra vi phạm.', GETDATE()),
(3, 6, N'Yêu cầu hoàn trả đang được xử lý.', GETDATE());

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
