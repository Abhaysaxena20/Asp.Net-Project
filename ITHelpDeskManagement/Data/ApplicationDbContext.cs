
using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITHelpDeskManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<TicketStatusHistory> TicketStatusHistories { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<TicketComment> ticketcomment { get; set; }

        public DbSet<CommentHiddenForUser> CommentHiddenForUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ticket assignment relationship
            builder.Entity<Ticket>()
                .HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket comments table
            builder.Entity<TicketComment>()
                .ToTable("ticketcomment");

            // Comment hidden for users relationship
            builder.Entity<CommentHiddenForUser>()
                .HasOne(h => h.Comment)
                .WithMany(c => c.HiddenForUsers)
                .HasForeignKey(h => h.CommentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
