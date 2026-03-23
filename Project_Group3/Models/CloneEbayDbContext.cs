﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PRN222_Group3.Models;

public partial class CloneEbayDbContext : DbContext
{
    public CloneEbayDbContext()
    {
    }

    public CloneEbayDbContext(DbContextOptions<CloneEbayDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Dispute> Disputes { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderTable> OrderTables { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ShippingInfo> ShippingInfos { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<StoreUpgradeRequest> StoreUpgradeRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }


    public virtual DbSet<RiskAssessment> RiskAssessments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var envConn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (envConn != null)
            {
                optionsBuilder.UseSqlServer(envConn);
            }
            else
            {
                Console.WriteLine("non env");
                var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                string Conectionstr = config.GetConnectionString("DefaultConnectionStringDB");
                optionsBuilder.UseSqlServer(Conectionstr);
            }


        }

    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Address__3213E83F1732EE1D");

            entity.ToTable("Address");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("fullName");
            entity.Property(e => e.IsDefault).HasColumnName("isDefault");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .HasColumnName("state");
            entity.Property(e => e.Street)
                .HasMaxLength(100)
                .HasColumnName("street");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Address__userId__3A81B327");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bid__3213E83F5791F39F");

            entity.ToTable("Bid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BidTime)
                .HasColumnType("datetime")
                .HasColumnName("bidTime");
            entity.Property(e => e.BidderId).HasColumnName("bidderId");
            entity.Property(e => e.ProductId).HasColumnName("productId");

            entity.HasOne(d => d.Bidder).WithMany(p => p.Bids)
                .HasForeignKey(d => d.BidderId)
                .HasConstraintName("FK__Bid__bidderId__5629CD9C");

            entity.HasOne(d => d.Product).WithMany(p => p.Bids)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Bid__productId__5535A963");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3213E83F0538EFE4");

            entity.ToTable("Category");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Coupon__3213E83FF0A24E75");

            entity.ToTable("Coupon");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discountPercent");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("endDate");
            entity.Property(e => e.MaxUsage).HasColumnName("maxUsage");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("startDate");

            entity.HasOne(d => d.Product).WithMany(p => p.Coupons)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Coupon__productI__60A75C0F");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Dispute__3213E83F33A52EF6");

            entity.ToTable("Dispute");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.RaisedBy).HasColumnName("raisedBy");
            entity.Property(e => e.Resolution).HasColumnName("resolution");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Dispute__orderId__693CA210");

            entity.HasOne(d => d.RaisedByNavigation).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.RaisedBy)
                .HasConstraintName("FK__Dispute__raisedB__6A30C649");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83F35952237");

            entity.ToTable("Feedback");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AverageRating)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("averageRating");
            entity.Property(e => e.PositiveRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("positiveRate");
            entity.Property(e => e.SellerId).HasColumnName("sellerId");
            entity.Property(e => e.TotalReviews).HasColumnName("totalReviews");

            entity.HasOne(d => d.Seller).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Feedback__seller__66603565");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Inventor__3213E83FEA820EB0");

            entity.ToTable("Inventory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LastUpdated)
                .HasColumnType("datetime")
                .HasColumnName("lastUpdated");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Inventory__produ__6383C8BA");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Message__3213E83F6ACF2C3A");

            entity.ToTable("Message");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ReceiverId).HasColumnName("receiverId");
            entity.Property(e => e.SenderId).HasColumnName("senderId");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("FK__Message__receive__5DCAEF64");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("FK__Message__senderI__5CD6CB2B");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3213E83F2CEFD87E");

            entity.ToTable("OrderItem");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unitPrice");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__order__46E78A0C");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderItem__produ__47DBAE45");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderTab__3213E83FF57DA510");

            entity.ToTable("OrderTable");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddressId).HasColumnName("addressId");
            entity.Property(e => e.BuyerId).HasColumnName("buyerId");
            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime")
                .HasColumnName("orderDate");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("totalPrice");

            entity.HasOne(d => d.Address).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK__OrderTabl__addre__440B1D61");

            entity.HasOne(d => d.Buyer).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__OrderTabl__buyer__4316F928");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3213E83F86B8F227");

            entity.ToTable("Payment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paidAt");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payment__orderId__4AB81AF0");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Payment__userId__4BAC3F29");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3213E83F1790EC51");

            entity.ToTable("Product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuctionEndTime)
                .HasColumnType("datetime")
                .HasColumnName("auctionEndTime");
            entity.Property(e => e.CategoryId).HasColumnName("categoryId");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.IsAuction).HasColumnName("isAuction");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("sellerId");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Product__categor__3F466844");

            entity.HasOne(d => d.Seller).WithMany(p => p.Products)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Product__sellerI__403A8C7D");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnRe__3213E83FC9C056B3");

            entity.ToTable("ReturnRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Images)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Order).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__ReturnReq__order__5165187F");

            entity.HasOne(d => d.User).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ReturnReq__userI__52593CB8");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Review__3213E83F3848BC43");

            entity.ToTable("Review");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewerId");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Review__productI__59063A47");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("FK__Review__reviewer__59FA5E80");
        });

        modelBuilder.Entity<ShippingInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipping__3213E83F445686F7");

            entity.ToTable("ShippingInfo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Carrier)
                .HasMaxLength(100)
                .HasColumnName("carrier");
            entity.Property(e => e.EstimatedArrival)
                .HasColumnType("datetime")
                .HasColumnName("estimatedArrival");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("trackingNumber");

            entity.HasOne(d => d.Order).WithMany(p => p.ShippingInfos)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__ShippingI__order__4E88ABD4");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Store__3213E83FF3F45B3E");

            entity.ToTable("Store");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BannerImageUrl).HasColumnName("bannerImageURL");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SellerId).HasColumnName("sellerId");
            entity.Property(e => e.StoreLevel)
                .HasDefaultValue((byte)1)
                .HasColumnName("storeLevel");
            entity.Property(e => e.StoreName)
                .HasMaxLength(100)
                .HasColumnName("storeName");

            entity.HasOne(d => d.Seller).WithMany(p => p.Stores)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Store__sellerId__6D0D32F4");
        });

        modelBuilder.Entity<StoreUpgradeRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StoreUpg__3213E83FBF979EB9");

            entity.ToTable("StoreUpgradeRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
            entity.Property(e => e.DecidedAt).HasColumnName("decidedAt");
            entity.Property(e => e.DecidedByAdminId).HasColumnName("decidedByAdminId");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.RequestedLevel).HasColumnName("requestedLevel");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.StoreId).HasColumnName("storeId");

            entity.HasOne(d => d.DecidedByAdmin).WithMany(p => p.StoreUpgradeRequests)
                .HasForeignKey(d => d.DecidedByAdminId)
                .HasConstraintName("FK_StoreUpgradeRequest_AdminUser");

            entity.HasOne(d => d.Store).WithMany(p => p.StoreUpgradeRequests)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoreUpgradeRequest_Store");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F45269D48");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E6164DEB5CBA9").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatarURL");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IsApproved).HasColumnName("isApproved");
            entity.Property(e => e.IsLocked).HasColumnName("isLocked");
            entity.Property(e => e.IsTwoFactorEnabled)
                .HasDefaultValue(false)
                .HasColumnName("isTwoFactorEnabled");
            entity.Property(e => e.LockedAt).HasColumnName("lockedAt");
            entity.Property(e => e.LockedReason)
                .HasMaxLength(200)
                .HasColumnName("lockedReason");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.TwoFactorRecoveryCodes)
                .HasMaxLength(500)
                .HasColumnName("twoFactorRecoveryCodes");
            entity.Property(e => e.TwoFactorSecret)
                .HasMaxLength(255)
                .HasColumnName("twoFactorSecret");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RiskAssessment__3213E83F");

            entity.ToTable("RiskAssessment");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.RiskScore).HasColumnName("RiskScore");
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .HasColumnName("RiskLevel");
            entity.Property(e => e.RecommendedAction)
                .HasMaxLength(20)
                .HasColumnName("RecommendedAction");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("Reason");
            entity.Property(e => e.IpMatchWithExistingAccount).HasColumnName("IpMatchWithExistingAccount");
            entity.Property(e => e.NewAccount).HasColumnName("NewAccount");
            entity.Property(e => e.SameEmailDomain).HasColumnName("SameEmailDomain");
            entity.Property(e => e.OutsideBusinessHours).HasColumnName("OutsideBusinessHours");
            entity.Property(e => e.DisposableEmail).HasColumnName("DisposableEmail");
            entity.Property(e => e.RapidRegistrations).HasColumnName("RapidRegistrations");
            entity.Property(e => e.ExistingAccountsWithSameIp).HasColumnName("ExistingAccountsWithSameIp");
            entity.Property(e => e.DaysSinceRegistration).HasColumnName("DaysSinceRegistration");
            entity.Property(e => e.AssessmentIpAddress)
                .HasMaxLength(50)
                .HasColumnName("AssessmentIpAddress");
            entity.Property(e => e.AssessmentDate)
                .HasColumnType("datetime")
                .HasColumnName("AssessmentDate");

            entity.HasOne(d => d.User).WithMany(p => p.RiskAssessments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RiskAssessment_User");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}