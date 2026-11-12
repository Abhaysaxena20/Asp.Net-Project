
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHelpDeskManagement.Models
{
    public class CommentHiddenForUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [ForeignKey(nameof(CommentId))]
        public TicketComment? Comment { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }
    }
}
