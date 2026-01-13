using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class RiskAssessment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RiskScore { get; set; }

    public string RiskLevel { get; set; } = null!;

    public string RecommendedAction { get; set; } = null!;

    public string? Reason { get; set; }

    public bool IpMatchWithExistingAccount { get; set; }

    public bool NewAccount { get; set; }

    public bool SameEmailDomain { get; set; }

    public bool OutsideBusinessHours { get; set; }

    public bool DisposableEmail { get; set; }

    public bool RapidRegistrations { get; set; }

    public int ExistingAccountsWithSameIp { get; set; }

    public int DaysSinceRegistration { get; set; }

    public string? AssessmentIpAddress { get; set; }

    public DateTime AssessmentDate { get; set; }

    public virtual User User { get; set; } = null!;
}
