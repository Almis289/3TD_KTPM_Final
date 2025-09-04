using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Store.Models
{
    public class PaymentHistory
    {
        [Key]
        public int PaymentHistoryId { get; set; }

        [ForeignKey("Payment")]
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } = null!;  // e.g., Success, Failed

        public string? Notes { get; set; }
    }
}
