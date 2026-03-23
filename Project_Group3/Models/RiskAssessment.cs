using System;

namespace PRN222_Group3.Models
{
    /// <summary>
    /// Risk Assessment entity - stores risk scoring history
    /// </summary>
    public partial class RiskAssessment
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RiskScore { get; set; }

        public string RiskLevel { get; set; } = "Low";

        public string RecommendedAction { get; set; } = "Allow";

        public string? Reason { get; set; }

        // Risk Factors
        public bool IpMatchWithExistingAccount { get; set; }

        public bool NewAccount { get; set; }

        public bool SameEmailDomain { get; set; }

        public bool OutsideBusinessHours { get; set; }

        public bool DisposableEmail { get; set; }

        public bool RapidRegistrations { get; set; }

        // Additional details
        public int ExistingAccountsWithSameIp { get; set; }

        public int DaysSinceRegistration { get; set; }

        public string? AssessmentIpAddress { get; set; }

        public DateTime AssessmentDate { get; set; }

        // Navigation property
        public virtual User? User { get; set; }
    }
}
