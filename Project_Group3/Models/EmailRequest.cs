namespace PRN222_Group3.Models
{
    public class EmailRequest
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? ToRole { get; set; } // "All", "SuperAdmin", "Moderator", "Support", "Ops"
        public List<string>? ToEmails { get; set; }
    }
}
