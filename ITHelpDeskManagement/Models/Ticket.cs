
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ITHelpDeskManagement.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Open";

        public string? AssignedTo { get; set; }

        [Required]
        public string Priority { get; set; } = "Medium";

        // Ticket creator
        public string? CreatedByUserId { get; set; }

        public string? CreatedByEmail { get; set; }

        // Assigned technician
        public string? AssignedToUserId { get; set; }

        public ApplicationUser? AssignedToUser { get; set; }

        // Ticket creation date
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Ticket comments
        public ICollection<TicketComment> Comments { get; set; }
            = new List<TicketComment>();
    }
}
