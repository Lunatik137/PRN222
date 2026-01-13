using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class User
{
    public int id { get; set; }

    public string? username { get; set; }

    public string? email { get; set; }

    public string? password { get; set; }

    public string? role { get; set; }

    public string? avatarURL { get; set; }

    public bool isApproved { get; set; }

    public bool isLocked { get; set; }

    public DateTime? lockedAt { get; set; }

    public string? lockedReason { get; set; }

    public DateTime createdAt { get; set; }

    public string? twoFactorSecret { get; set; }

    public bool? isTwoFactorEnabled { get; set; }

    public string? twoFactorRecoveryCodes { get; set; }

    public string? registrationIP { get; set; }

    public string? lastLoginIP { get; set; }

    public DateTime? lastLoginTimestamp { get; set; }

    public int? RiskScore { get; set; }

    public string? RiskLevel { get; set; }

    public DateTime? LastRiskAssessment { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Message> Messagereceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> Messagesenders { get; set; } = new List<Message>();

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();

    public virtual ICollection<StoreUpgradeRequest> StoreUpgradeRequests { get; set; } = new List<StoreUpgradeRequest>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
}
