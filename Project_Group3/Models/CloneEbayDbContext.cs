using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Project_Group3.Models;

public partial class CloneEbayDbContext : DbContext
{
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

    public virtual DbSet<RiskAssessment> RiskAssessments { get; set; }

    public virtual DbSet<ShippingInfo> ShippingInfos { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<StoreUpgradeRequest> StoreUpgradeRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Address__3213E83F1732EE1D");

            entity.ToTable("Address");

            entity.Property(e => e.city)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.country)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.fullName)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.phone)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.state)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.street)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.user).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__Address__userId__3A81B327");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Bid__3213E83F5791F39F");

            entity.ToTable("Bid");

            entity.Property(e => e.amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.bidTime).HasColumnType("datetime");

            entity.HasOne(d => d.bidder).WithMany(p => p.Bids)
                .HasForeignKey(d => d.bidderId)
                .HasConstraintName("FK__Bid__bidderId__5629CD9C");

            entity.HasOne(d => d.product).WithMany(p => p.Bids)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Bid__productId__5535A963");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Category__3213E83F0538EFE4");

            entity.ToTable("Category");

            entity.Property(e => e.name)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Coupon__3213E83FF0A24E75");

            entity.ToTable("Coupon");

            entity.Property(e => e.code)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.discountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.endDate).HasColumnType("datetime");
            entity.Property(e => e.startDate).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Coupons)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Coupon__productI__60A75C0F");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Dispute__3213E83F33A52EF6");

            entity.ToTable("Dispute");

            entity.Property(e => e.description).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.resolution).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.order).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__Dispute__orderId__693CA210");

            entity.HasOne(d => d.raisedByNavigation).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.raisedBy)
                .HasConstraintName("FK__Dispute__raisedB__6A30C649");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Feedback__3213E83F35952237");

            entity.ToTable("Feedback");

            entity.Property(e => e.averageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.positiveRate).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.seller).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Feedback__seller__66603565");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Inventor__3213E83FEA820EB0");

            entity.ToTable("Inventory");

            entity.Property(e => e.lastUpdated).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Inventory__produ__6383C8BA");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Message__3213E83F6ACF2C3A");

            entity.ToTable("Message");

            entity.Property(e => e.content).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.receiver).WithMany(p => p.Messagereceivers)
                .HasForeignKey(d => d.receiverId)
                .HasConstraintName("FK__Message__receive__5DCAEF64");

            entity.HasOne(d => d.sender).WithMany(p => p.Messagesenders)
                .HasForeignKey(d => d.senderId)
                .HasConstraintName("FK__Message__senderI__5CD6CB2B");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OrderIte__3213E83F2CEFD87E");

            entity.ToTable("OrderItem");

            entity.Property(e => e.unitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__OrderItem__order__46E78A0C");

            entity.HasOne(d => d.product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__OrderItem__produ__47DBAE45");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OrderTab__3213E83FF57DA510");

            entity.ToTable("OrderTable");

            entity.Property(e => e.orderDate).HasColumnType("datetime");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.totalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.address).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.addressId)
                .HasConstraintName("FK__OrderTabl__addre__440B1D61");

            entity.HasOne(d => d.buyer).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.buyerId)
                .HasConstraintName("FK__OrderTabl__buyer__4316F928");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Payment__3213E83F86B8F227");

            entity.ToTable("Payment");

            entity.Property(e => e.amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.method)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.paidAt).HasColumnType("datetime");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__Payment__orderId__4AB81AF0");

            entity.HasOne(d => d.user).WithMany(p => p.Payments)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__Payment__userId__4BAC3F29");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Product__3213E83F1790EC51");

            entity.ToTable("Product");

            entity.Property(e => e.auctionEndTime).HasColumnType("datetime");
            entity.Property(e => e.description).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.images).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.title)
                .HasMaxLength(255)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.category).WithMany(p => p.Products)
                .HasForeignKey(d => d.categoryId)
                .HasConstraintName("FK__Product__categor__3F466844");

            entity.HasOne(d => d.seller).WithMany(p => p.Products)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Product__sellerI__403A8C7D");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ReturnRe__3213E83FC9C056B3");

            entity.ToTable("ReturnRequest");

            entity.Property(e => e.Images)
                .HasMaxLength(100)
                .IsUnicode(false)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.createdAt).HasColumnType("datetime");
            entity.Property(e => e.reason).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.order).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__ReturnReq__order__5165187F");

            entity.HasOne(d => d.user).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__ReturnReq__userI__52593CB8");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Review__3213E83F3848BC43");

            entity.ToTable("Review");

            entity.Property(e => e.comment).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.createdAt).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Review__productI__59063A47");

            entity.HasOne(d => d.reviewer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.reviewerId)
                .HasConstraintName("FK__Review__reviewer__59FA5E80");
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RiskAsse__3214EC07F21B3ADC");

            entity.ToTable("RiskAssessment");

            entity.HasIndex(e => e.AssessmentDate, "IX_RiskAssessment_AssessmentDate").HasFillFactor(100);

            entity.HasIndex(e => e.UserId, "IX_RiskAssessment_UserId").HasFillFactor(100);

            entity.Property(e => e.AssessmentDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.AssessmentIpAddress)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.RecommendedAction)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.User).WithMany(p => p.RiskAssessments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RiskAssessment_User");
        });

        modelBuilder.Entity<ShippingInfo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F445686F7");

            entity.ToTable("ShippingInfo");

            entity.Property(e => e.carrier)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.estimatedArrival).HasColumnType("datetime");
            entity.Property(e => e.status)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.trackingNumber)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.order).WithMany(p => p.ShippingInfos)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__ShippingI__order__4E88ABD4");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Store__3213E83FF3F45B3E");

            entity.ToTable("Store");

            entity.Property(e => e.bannerImageURL).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.description).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.storeLevel).HasDefaultValue((byte)1);
            entity.Property(e => e.storeName)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.seller).WithMany(p => p.Stores)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Store__sellerId__6D0D32F4");
        });

        modelBuilder.Entity<StoreUpgradeRequest>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__StoreUpg__3213E83FBF979EB9");

            entity.ToTable("StoreUpgradeRequest");

            entity.Property(e => e.createdAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.note)
                .HasMaxLength(500)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasOne(d => d.decidedByAdmin).WithMany(p => p.StoreUpgradeRequests)
                .HasForeignKey(d => d.decidedByAdminId)
                .HasConstraintName("FK_StoreUpgradeRequest_AdminUser");

            entity.HasOne(d => d.store).WithMany(p => p.StoreUpgradeRequests)
                .HasForeignKey(d => d.storeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoreUpgradeRequest_Store");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__User__3213E83F45269D48");

            entity.ToTable("User");

            entity.HasIndex(e => e.email, "UQ__User__AB6E6164DEB5CBA9").IsUnique();

            entity.Property(e => e.LastRiskAssessment).HasColumnType("datetime");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.RiskScore).HasDefaultValue(0);
            entity.Property(e => e.avatarURL).UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.createdAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.email)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.isTwoFactorEnabled).HasDefaultValue(false);
            entity.Property(e => e.lastLoginIP)
                .HasMaxLength(45)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.lastLoginTimestamp).HasColumnType("datetime");
            entity.Property(e => e.lockedReason)
                .HasMaxLength(200)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.password)
                .HasMaxLength(255)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.registrationIP)
                .HasMaxLength(45)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.twoFactorRecoveryCodes)
                .HasMaxLength(500)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.twoFactorSecret)
                .HasMaxLength(255)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.username)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
