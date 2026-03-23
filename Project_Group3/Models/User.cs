using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsApproved { get; set; }

    public bool IsLocked { get; set; }

    public string? LockedReason { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? TwoFactorSecret { get; set; }

    public bool? IsTwoFactorEnabled { get; set; }

    public string? TwoFactorRecoveryCodes { get; set; }

    public string? RegistrationIp { get; set; }

    public string? LastLoginIp { get; set; }

    public DateTime? LastLoginTimestamp { get; set; }

    public string? Phone { get; set; }

    public int? RiskScore { get; set; }

    public string? RiskLevel { get; set; }

    public DateTime? LastRiskAssessment { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    public virtual ICollection<StoreUpgradeRequest> StoreUpgradeRequests { get; set; } = new List<StoreUpgradeRequest>();
}

