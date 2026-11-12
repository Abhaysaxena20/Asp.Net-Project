
using System;

namespace ITHelpDeskManagement.ViewModels
{
    public class AddCommentHistoryViewModel
    {
        public int CommentId { get; set; }

        public int TicketId { get; set; }

        public string TicketTitle { get; set; } = string.Empty;

        public string AuthorEmail { get; set; } = string.Empty;

        public string AuthorRole { get; set; } = string.Empty;

        public string TicketCreatorEmail { get; set; } = string.Empty;

        public string CommentText { get; set; } = string.Empty;

        public DateTime CommentDate { get; set; }

        public bool NotificationSent { get; set; }

        public bool? NotificationRead { get; set; }
    }
}
