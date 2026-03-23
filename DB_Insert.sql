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
('buyer02',    'buyer02@cloneebay.com',    'hashed_pw', 'Buyer',  0, 0, 0, DATEADD(DAY,-2,GETDATE()),  '118.70.1.6'),
('seller_mid', 'seller_mid@cloneebay.com', 'hashed_pw', 'Seller', 1, 0, 1, DATEADD(MONTH,-1,GETDATE()), '113.22.1.12'),
('seller_new', 'seller_new@cloneebay.com', 'hashed_pw', 'Seller', 1, 0, 0, DATEADD(DAY,-25,GETDATE()),  '113.22.1.13'),
('buyer03',    'buyer03@cloneebay.com',    'hashed_pw', 'Buyer',  1, 0, 1, DATEADD(DAY,-18,GETDATE()),  '118.70.1.7'),
('buyer04',    'buyer04@cloneebay.com',    'hashed_pw', 'Buyer',  1, 0, 0, DATEADD(DAY,-5,GETDATE()),   '118.70.1.8');

/* =====================================================
2. CATEGORY
===================================================== */
INSERT INTO Category (name)
VALUES (N'Electronics'), (N'Fashion'), (N'Collectibles'), (N'Home & Living'), (N'Sports');

/* =====================================================
3. STORE + PRODUCT (CÓ SẢN PHẨM VI PHẠM)
===================================================== */
INSERT INTO Store (sellerId, storeName, description, storeLevel)
VALUES
(4, N'Store Vi Phạm', N'Shop nhiều report', 1),
(5, N'Store Uy Tín',  N'Shop chính hãng',   2),
(8, N'Store Tầm Trung', N'Đa dạng sản phẩm, kiểm soát chất lượng ổn định', 2),
(9, N'Store Mới', N'Shop mới mở, giá cạnh tranh', 1);

INSERT INTO Product
(title, description, price, categoryId, sellerId, isAuction, status)
VALUES
(N'Fake iPhone', N'Nghi hàng giả', 200, 1, 4, 0, N'REPORTED'),
(N'Original iPhone', N'Chính hãng', 1200,1, 5, 0, N'ACTIVE'),
(N'Rare Coin', N'Đấu giá', 500, 3, 5, 1, N'AUCTION'),
(N'Luxury Handbag Replica', N'Nghi ngờ hàng nhái thương hiệu', 350, 2, 4, 0, N'REPORTED'),
(N'Gaming Mouse Pro', N'Chuột gaming mới 100%', 85, 1, 8, 0, N'ACTIVE'),
(N'Vintage Jersey', N'Áo đấu sưu tầm phiên bản hiếm', 220, 3, 8, 1, N'REPORTED'),
(N'Air Fryer 6L', N'Nồi chiên không dầu gia đình', 150, 4, 9, 0, N'ACTIVE'),
(N'Yoga Mat Premium', N'Thảm yoga chống trượt', 35, 5, 9, 0, N'HIDDEN');

/* =====================================================
4. INVENTORY
===================================================== */
INSERT INTO Inventory (productId, quantity, lastUpdated)
VALUES
(1, 20, GETDATE()),
(2, 5,  GETDATE()),
(3, 1,  GETDATE()),
(4, 12, GETDATE()),
(5, 30, GETDATE()),
(6, 2,  GETDATE()),
(7, 15, GETDATE()),
(8, 0,  GETDATE());

/* =====================================================
5. ADDRESS + ORDER + PAYMENT (DỮ LIỆU THEO THỜI GIAN)
===================================================== */
INSERT INTO Address
(userId, fullName, phone, street, city, state, country, isDefault)
VALUES
(6, N'Buyer One', '090000001', N'1 Le Loi', N'HCM', N'Q1', N'VN', 1),
(10, N'Buyer Three', '090000003', N'99 Tran Hung Dao', N'Ha Noi', N'Hoan Kiem', N'VN', 1),
(11, N'Buyer Four', '090000004', N'15 Hai Ba Trung', N'Da Nang', N'Hai Chau', N'VN', 1);

INSERT INTO OrderTable
(buyerId, addressId, orderDate, totalPrice, status)
VALUES
(6, 1, DATEADD(DAY,-7,GETDATE()), 1200, N'PAID'),
(6, 1, DATEADD(MONTH,-1,GETDATE()), 500, N'PAID'),
(6, 1, DATEADD(MONTH,-3,GETDATE()), 200, N'REFUNDED'),
(10, 2, DATEADD(DAY,-12,GETDATE()), 85, N'PAID'),
(10, 2, DATEADD(DAY,-3,GETDATE()), 220, N'DISPUTED'),
(11, 3, DATEADD(DAY,-1,GETDATE()), 150, N'SHIPPED');

INSERT INTO Payment
(orderId, userId, amount, method, status, paidAt)
VALUES
(1, 6, 1200, 'CARD', 'SUCCESS', DATEADD(DAY,-7,GETDATE())),
(2, 6, 500,  'CARD', 'SUCCESS', DATEADD(MONTH,-1,GETDATE())),
(3, 6, 200,  'CARD', 'REFUNDED',DATEADD(MONTH,-3,GETDATE())),
(4, 10, 85,  'EWALLET', 'SUCCESS', DATEADD(DAY,-12,GETDATE())),
(5, 10, 220, 'CARD', 'PENDING', DATEADD(DAY,-3,GETDATE())),
(6, 11, 150, 'BANK_TRANSFER', 'SUCCESS', DATEADD(DAY,-1,GETDATE()));

/* =====================================================
6. REVIEW + FEEDBACK (GIÁM SÁT ĐÁNH GIÁ)
===================================================== */
INSERT INTO Review
(productId, reviewerId, rating, comment, createdAt)
VALUES
(1, 6, 1, N'Hàng giả', GETDATE()),
(2, 6, 5, N'Rất tốt', GETDATE()),
(1, 10, 2, N'Chất lượng không như mô tả', DATEADD(DAY,-2,GETDATE())),
(4, 11, 1, N'Nghi hàng nhái, đóng gói kém', DATEADD(DAY,-1,GETDATE())),
(6, 10, 2, N'Hình ảnh khác sản phẩm thực tế', DATEADD(DAY,-4,GETDATE())),
(5, 11, 5, N'Sản phẩm tốt, giao nhanh', DATEADD(DAY,-6,GETDATE())),
(7, 6, 4, N'Dùng ổn trong tầm giá', DATEADD(DAY,-2,GETDATE()));

INSERT INTO Feedback
(sellerId, averageRating, totalReviews, positiveRate)
VALUES
(4, 1.5, 10, 10),
(5, 4.8, 120, 96),
(8, 3.9, 34, 78),
(9, 4.2, 12, 84);

/* =====================================================
7. DISPUTE + RETURN REQUEST (ADMIN XỬ LÝ)
===================================================== */
INSERT INTO Dispute
(orderId, raisedBy, description, status)
VALUES
(1, 6, N'Nghi ngờ hàng giả', 'PENDING'),
(5, 10, N'Sản phẩm sưu tầm có dấu hiệu không đúng mô tả', 'OPEN');

INSERT INTO ReturnRequest
(orderId, userId, reason, status, createdAt)
VALUES
(1, 6, N'Hoàn tiền do vi phạm', 'PENDING', GETDATE()),
(5, 10, N'Yêu cầu trả hàng vì nghi vấn chất lượng', 'APPROVED', DATEADD(DAY,-1,GETDATE()));

/* =====================================================
8. RISK ASSESSMENT (SECURITY ADMIN)
===================================================== */
INSERT INTO RiskAssessment
(UserId, RiskScore, RiskLevel, RecommendedAction, Reason,
 RapidRegistrations, DisposableEmail)
VALUES
(4, 90, 'HIGH', 'LOCK', N'Nhiều report + dispute', 1, 1),
(8, 45, 'MEDIUM', 'MONITOR', N'Một số đánh giá thấp cần theo dõi', 0, 0),
(9, 20, 'LOW', 'ALLOW', N'Tài khoản mới, chưa có tín hiệu rủi ro lớn', 0, 0);

/* =====================================================
9. MESSAGE (ADMIN GỬI CẢNH BÁO)
===================================================== */
INSERT INTO Message
(senderId, receiverId, content, [timestamp])
VALUES
(1, 4, N'Tài khoản bạn đang bị điều tra vi phạm.', GETDATE()),
(3, 6, N'Yêu cầu hoàn trả đang được xử lý.', GETDATE()),
(1, 8, N'Nhắc nhở: vui lòng tăng chất lượng mô tả sản phẩm để tránh report.', DATEADD(DAY,-1,GETDATE())),
(2, 9, N'Chào mừng shop mới, hãy tuân thủ chính sách đăng bán.', DATEADD(DAY,-2,GETDATE())),
(3, 10, N'Tranh chấp đơn hàng #5 đã được ghi nhận và đang xử lý.', DATEADD(HOUR,-12,GETDATE()));

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO