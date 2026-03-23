-- DROP SCHEMA dbo;
CREATE DATABASE CloneEbayDB;
GO

USE CloneEbayDB;
GO
-- CloneEbayDB.dbo.Category definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Category;

CREATE TABLE Category (
	id int IDENTITY(1,1) NOT NULL,
	name nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Category__3213E83F0538EFE4 PRIMARY KEY (id)
);


-- CloneEbayDB.dbo.[User] definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.[User];

CREATE TABLE [User] (
	id int IDENTITY(1,1) NOT NULL,
	username nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	email nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	password nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[role] nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	avatarURL nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	isApproved bit DEFAULT 0 NOT NULL,
	isLocked bit DEFAULT 0 NOT NULL,
	lockedAt datetime2 NULL,
	lockedReason nvarchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	createdAt datetime2 DEFAULT sysdatetime() NOT NULL,
	twoFactorSecret nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	isTwoFactorEnabled bit DEFAULT 0 NULL,
	twoFactorRecoveryCodes nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	registrationIP nvarchar(45) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	lastLoginIP nvarchar(45) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	lastLoginTimestamp datetime NULL,
	RiskScore int DEFAULT 0 NULL,
	RiskLevel nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LastRiskAssessment datetime NULL,
	Phone nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__User__3213E83F45269D48 PRIMARY KEY (id),
	CONSTRAINT UQ__User__AB6E6164DEB5CBA9 UNIQUE (email)
);


-- CloneEbayDB.dbo.Address definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Address;

CREATE TABLE Address (
	id int IDENTITY(1,1) NOT NULL,
	userId int NULL,
	fullName nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	phone nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	street nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	city nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	state nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	country nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	isDefault bit NULL,
	CONSTRAINT PK__Address__3213E83F1732EE1D PRIMARY KEY (id),
	CONSTRAINT FK__Address__userId__3A81B327 FOREIGN KEY (userId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Feedback definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Feedback;

CREATE TABLE Feedback (
	id int IDENTITY(1,1) NOT NULL,
	sellerId int NULL,
	averageRating decimal(3,2) NULL,
	totalReviews int NULL,
	positiveRate decimal(5,2) NULL,
	CONSTRAINT PK__Feedback__3213E83F35952237 PRIMARY KEY (id),
	CONSTRAINT FK__Feedback__seller__66603565 FOREIGN KEY (sellerId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Message definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Message;

CREATE TABLE Message (
	id int IDENTITY(1,1) NOT NULL,
	senderId int NULL,
	receiverId int NULL,
	content nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[timestamp] datetime NULL,
	CONSTRAINT PK__Message__3213E83F6ACF2C3A PRIMARY KEY (id),
	CONSTRAINT FK__Message__receive__5DCAEF64 FOREIGN KEY (receiverId) REFERENCES [User](id),
	CONSTRAINT FK__Message__senderI__5CD6CB2B FOREIGN KEY (senderId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.OrderTable definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.OrderTable;

CREATE TABLE OrderTable (
	id int IDENTITY(1,1) NOT NULL,
	buyerId int NULL,
	addressId int NULL,
	orderDate datetime NULL,
	totalPrice decimal(10,2) NULL,
	status nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__OrderTab__3213E83FF57DA510 PRIMARY KEY (id),
	CONSTRAINT FK__OrderTabl__addre__440B1D61 FOREIGN KEY (addressId) REFERENCES Address(id),
	CONSTRAINT FK__OrderTabl__buyer__4316F928 FOREIGN KEY (buyerId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Payment definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Payment;

CREATE TABLE Payment (
	id int IDENTITY(1,1) NOT NULL,
	orderId int NULL,
	userId int NULL,
	amount decimal(10,2) NULL,
	[method] nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	status nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	paidAt datetime NULL,
	CONSTRAINT PK__Payment__3213E83F86B8F227 PRIMARY KEY (id),
	CONSTRAINT FK__Payment__orderId__4AB81AF0 FOREIGN KEY (orderId) REFERENCES OrderTable(id),
	CONSTRAINT FK__Payment__userId__4BAC3F29 FOREIGN KEY (userId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Product definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Product;

CREATE TABLE Product (
	id int IDENTITY(1,1) NOT NULL,
	title nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	price decimal(10,2) NULL,
	images nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	categoryId int NULL,
	sellerId int NULL,
	isAuction bit NULL,
	auctionEndTime datetime NULL,
	status nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Product__3213E83F1790EC51 PRIMARY KEY (id),
	CONSTRAINT FK__Product__categor__3F466844 FOREIGN KEY (categoryId) REFERENCES Category(id),
	CONSTRAINT FK__Product__sellerI__403A8C7D FOREIGN KEY (sellerId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.ReturnRequest definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.ReturnRequest;

CREATE TABLE ReturnRequest (
	id int IDENTITY(1,1) NOT NULL,
	orderId int NULL,
	userId int NULL,
	reason nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	status nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	createdAt datetime NULL,
	Images varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__ReturnRe__3213E83FC9C056B3 PRIMARY KEY (id),
	CONSTRAINT FK__ReturnReq__order__5165187F FOREIGN KEY (orderId) REFERENCES OrderTable(id),
	CONSTRAINT FK__ReturnReq__userI__52593CB8 FOREIGN KEY (userId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Review definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Review;

CREATE TABLE Review (
	id int IDENTITY(1,1) NOT NULL,
	productId int NULL,
	reviewerId int NULL,
	rating int NULL,
	comment nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	createdAt datetime NULL,
	CONSTRAINT PK__Review__3213E83F3848BC43 PRIMARY KEY (id),
	CONSTRAINT FK__Review__productI__59063A47 FOREIGN KEY (productId) REFERENCES Product(id),
	CONSTRAINT FK__Review__reviewer__59FA5E80 FOREIGN KEY (reviewerId) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.RiskAssessment definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.RiskAssessment;

CREATE TABLE RiskAssessment (
	Id int IDENTITY(1,1) NOT NULL,
	UserId int NOT NULL,
	RiskScore int NOT NULL,
	RiskLevel nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	RecommendedAction nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Reason nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IpMatchWithExistingAccount bit DEFAULT 0 NOT NULL,
	NewAccount bit DEFAULT 0 NOT NULL,
	SameEmailDomain bit DEFAULT 0 NOT NULL,
	OutsideBusinessHours bit DEFAULT 0 NOT NULL,
	DisposableEmail bit DEFAULT 0 NOT NULL,
	RapidRegistrations bit DEFAULT 0 NOT NULL,
	ExistingAccountsWithSameIp int DEFAULT 0 NOT NULL,
	DaysSinceRegistration int DEFAULT 0 NOT NULL,
	AssessmentIpAddress nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	AssessmentDate datetime DEFAULT getutcdate() NOT NULL,
	CONSTRAINT PK__RiskAsse__3214EC07F21B3ADC PRIMARY KEY (Id),
	CONSTRAINT FK_RiskAssessment_User FOREIGN KEY (UserId) REFERENCES [User](id) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_RiskAssessment_AssessmentDate ON CloneEbayDB.dbo.RiskAssessment (  AssessmentDate ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX IX_RiskAssessment_UserId ON CloneEbayDB.dbo.RiskAssessment (  UserId ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- CloneEbayDB.dbo.ShippingInfo definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.ShippingInfo;

CREATE TABLE ShippingInfo (
	id int IDENTITY(1,1) NOT NULL,
	orderId int NULL,
	carrier nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	trackingNumber nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	status nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	estimatedArrival datetime NULL,
	CONSTRAINT PK__Shipping__3213E83F445686F7 PRIMARY KEY (id),
	CONSTRAINT FK__ShippingI__order__4E88ABD4 FOREIGN KEY (orderId) REFERENCES OrderTable(id)
);


-- CloneEbayDB.dbo.Store definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Store;

CREATE TABLE Store (
	id int IDENTITY(1,1) NOT NULL,
	sellerId int NULL,
	storeName nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	bannerImageURL nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	storeLevel tinyint DEFAULT 1 NOT NULL,
	CONSTRAINT PK__Store__3213E83FF3F45B3E PRIMARY KEY (id),
	CONSTRAINT FK__Store__sellerId__6D0D32F4 FOREIGN KEY (sellerId) REFERENCES [User](id)
);
ALTER TABLE CloneEbayDB.dbo.Store WITH NOCHECK ADD CONSTRAINT CK_Store_StoreLevel CHECK (([storeLevel]=(3) OR [storeLevel]=(2) OR [storeLevel]=(1)));


-- CloneEbayDB.dbo.StoreUpgradeRequest definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.StoreUpgradeRequest;

CREATE TABLE StoreUpgradeRequest (
	id int IDENTITY(1,1) NOT NULL,
	storeId int NOT NULL,
	requestedLevel tinyint NOT NULL,
	status tinyint DEFAULT 0 NOT NULL,
	note nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	createdAt datetime2 DEFAULT sysdatetime() NOT NULL,
	decidedAt datetime2 NULL,
	decidedByAdminId int NULL,
	CONSTRAINT PK__StoreUpg__3213E83FBF979EB9 PRIMARY KEY (id),
	CONSTRAINT FK_StoreUpgradeRequest_AdminUser FOREIGN KEY (decidedByAdminId) REFERENCES [User](id),
	CONSTRAINT FK_StoreUpgradeRequest_Store FOREIGN KEY (storeId) REFERENCES Store(id)
);
ALTER TABLE CloneEbayDB.dbo.StoreUpgradeRequest WITH NOCHECK ADD CONSTRAINT CK_StoreUpgradeRequest_RequestedLevel CHECK (([requestedLevel]=(3) OR [requestedLevel]=(2) OR [requestedLevel]=(1)));
ALTER TABLE CloneEbayDB.dbo.StoreUpgradeRequest WITH NOCHECK ADD CONSTRAINT CK_StoreUpgradeRequest_Status CHECK (([status]=(2) OR [status]=(1) OR [status]=(0)));


-- CloneEbayDB.dbo.Bid definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Bid;

CREATE TABLE Bid (
	id int IDENTITY(1,1) NOT NULL,
	productId int NULL,
	bidderId int NULL,
	amount decimal(10,2) NULL,
	bidTime datetime NULL,
	CONSTRAINT PK__Bid__3213E83F5791F39F PRIMARY KEY (id),
	CONSTRAINT FK__Bid__bidderId__5629CD9C FOREIGN KEY (bidderId) REFERENCES [User](id),
	CONSTRAINT FK__Bid__productId__5535A963 FOREIGN KEY (productId) REFERENCES Product(id)
);


-- CloneEbayDB.dbo.Coupon definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Coupon;

CREATE TABLE Coupon (
	id int IDENTITY(1,1) NOT NULL,
	code nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	discountPercent decimal(5,2) NULL,
	startDate datetime NULL,
	endDate datetime NULL,
	maxUsage int NULL,
	productId int NULL,
	CONSTRAINT PK__Coupon__3213E83FF0A24E75 PRIMARY KEY (id),
	CONSTRAINT FK__Coupon__productI__60A75C0F FOREIGN KEY (productId) REFERENCES Product(id)
);


-- CloneEbayDB.dbo.Dispute definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Dispute;

CREATE TABLE Dispute (
	id int IDENTITY(1,1) NOT NULL,
	orderId int NULL,
	raisedBy int NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	status nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	resolution nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Dispute__3213E83F33A52EF6 PRIMARY KEY (id),
	CONSTRAINT FK__Dispute__orderId__693CA210 FOREIGN KEY (orderId) REFERENCES OrderTable(id),
	CONSTRAINT FK__Dispute__raisedB__6A30C649 FOREIGN KEY (raisedBy) REFERENCES [User](id)
);


-- CloneEbayDB.dbo.Inventory definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.Inventory;

CREATE TABLE Inventory (
	id int IDENTITY(1,1) NOT NULL,
	productId int NULL,
	quantity int NULL,
	lastUpdated datetime NULL,
	CONSTRAINT PK__Inventor__3213E83FEA820EB0 PRIMARY KEY (id),
	CONSTRAINT FK__Inventory__produ__6383C8BA FOREIGN KEY (productId) REFERENCES Product(id)
);


-- CloneEbayDB.dbo.OrderItem definition

-- Drop table

-- DROP TABLE CloneEbayDB.dbo.OrderItem;

CREATE TABLE OrderItem (
	id int IDENTITY(1,1) NOT NULL,
	orderId int NULL,
	productId int NULL,
	quantity int NULL,
	unitPrice decimal(10,2) NULL,
	CONSTRAINT PK__OrderIte__3213E83F2CEFD87E PRIMARY KEY (id),
	CONSTRAINT FK__OrderItem__order__46E78A0C FOREIGN KEY (orderId) REFERENCES OrderTable(id),
	CONSTRAINT FK__OrderItem__produ__47DBAE45 FOREIGN KEY (productId) REFERENCES Product(id)
);


