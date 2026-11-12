using Microsoft.AspNetCore.SignalR;

namespace ITHelpDeskManagement.Hubs
{
    public class TicketHub : Hub
    {
        // Test method - keep this for now.
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        // Notify clients when a ticket status changes.
        public async Task NotifyTicketStatus(
            int ticketId,
            string status)
        {
            await Clients.All.SendAsync(
                "TicketStatusUpdated",
                ticketId,
                status
            );
        }
    }
}