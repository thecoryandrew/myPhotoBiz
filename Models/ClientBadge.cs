namespace MyPhotoBiz.Models
{
    public class ClientBadge
    {
        public int Id { get; set; }

        public int ClientProfileId { get; set; }
        public virtual ClientProfile ClientProfile { get; set; } = null!;

        public int BadgeId { get; set; }
        public virtual Badge Badge { get; set; } = null!;

        public DateTime EarnedDate { get; set; } = DateTime.UtcNow;

        public int? ContractId { get; set; } // Track which contract awarded this badge
        public virtual Contract? Contract { get; set; }

        public string? Notes { get; set; } // Optional notes about how/why badge was earned
    }
}
