using System;
using System.ComponentModel.DataAnnotations;

namespace ITHelpDeskManagement.Models
{
    public class TicketStatusHistory
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }

        [Required]
        public string OldStatus { get; set; } = string.Empty;

        [Required]
        public string NewStatus { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}