
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHelpDeskManagement.Models
{
    public class TicketComment
    {
        [Key]
        public int CommentId { get; set; }

        // Ticket to which this comment belongs
        [Required]
        public int TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        // User who posted the comment
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // Actual comment
        [Required]
        [StringLength(2000)]
        public string CommentText { get; set; } = string.Empty;

        // Comment creation date and time
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<CommentHiddenForUser> HiddenForUsers { get; set; }
            = new List<CommentHiddenForUser>();
    }
}
