
// ======================================
// 1. SIGNALR CONNECTION
// ======================================

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ticketHub")
    .withAutomaticReconnect()
    .build();


// ======================================
// 2. LISTEN FOR TICKET STATUS UPDATES
// ======================================

connection.on("TicketStatusUpdated", function (ticketId, status) {

    console.log("Ticket ID:", ticketId);
    console.log("New Status:", status);

    alert(
        "Ticket #" + ticketId +
        " status changed to " + status
    );

    // Refresh notifications
    loadDropdownNotifications();
    loadUnreadCount();
});


// ======================================
// 3. LISTEN FOR NEW ASSIGNMENT NOTIFICATIONS
// ======================================

connection.on(
    "ReceiveNotification",
    function (notificationId, message, ticketId, createdAt) {

        console.log("New notification received!");

        console.log("Notification ID:", notificationId);
        console.log("Message:", message);
        console.log("Ticket ID:", ticketId);
        console.log("Created At:", createdAt);

        // Show immediate notification
        alert(message);

        // Refresh notification dropdown
        loadDropdownNotifications();

        // Refresh unread notification count
        loadUnreadCount();
    }
);


// ======================================
// 4. SIGNALR CONNECTION EVENTS
// ======================================

connection.onreconnecting(function (error) {

    console.log("SignalR reconnecting...", error);

});

connection.onreconnected(function (connectionId) {

    console.log("SignalR reconnected:", connectionId);

    // Refresh notifications after reconnecting
    loadDropdownNotifications();
    loadUnreadCount();

});

connection.onclose(function (error) {

    console.error("SignalR connection closed:", error);

});


// ======================================
// 5. START SIGNALR CONNECTION
// ======================================

async function startSignalRConnection() {

    try {

        if (connection.state === signalR.HubConnectionState.Disconnected) {

            await connection.start();

            console.log("SignalR Connected Successfully!");

        }

    }
    catch (error) {

        console.error("SignalR Connection Failed:", error);

    }

}

startSignalRConnection();


// ======================================
// 6. LOAD NOTIFICATIONS
// ======================================

async function loadDropdownNotifications() {

    const list = document.getElementById("notificationList");

    if (!list) {
        return;
    }

    try {

        const response = await fetch("/Notification/GetMyNotifications");

        if (!response.ok) {
            throw new Error("HTTP error: " + response.status);
        }

        const notifications = await response.json();

        console.log("Dropdown notifications:", notifications);

        renderDropdownNotifications(notifications);

        await loadUnreadCount();

    }
    catch (error) {

        console.error("Notification loading failed:", error);

        list.replaceChildren();

        const li = document.createElement("li");

        li.className = "dropdown-item-text text-danger";
        li.textContent = "Unable to load notifications.";

        list.appendChild(li);

    }

}


// ======================================
// 7. DISPLAY NOTIFICATIONS
// ======================================

function renderDropdownNotifications(notifications) {

    const list = document.getElementById("notificationList");

    if (!list) {
        return;
    }

    list.replaceChildren();

    // Dropdown heading
    const header = document.createElement("li");

    header.className = "dropdown-header fw-bold";
    header.textContent = "Notifications";

    list.appendChild(header);


    // Latest 5 notifications
    const latestNotifications = notifications.slice(0, 5);


    if (latestNotifications.length === 0) {

        const li = document.createElement("li");

        const message = document.createElement("span");

        message.className = "dropdown-item-text text-muted";
        message.textContent = "No notifications found.";

        li.appendChild(message);

        list.appendChild(li);

    }
    else {

        latestNotifications.forEach(function (notification) {

            const li = document.createElement("li");

            const item = document.createElement("div");

            item.className = "dropdown-item text-wrap";


            // Notification message
            const message = document.createElement("div");

            message.className = "fw-semibold";
            message.textContent = notification.message;


            // Notification date
            const date = document.createElement("small");

            date.className = "text-muted";

            date.textContent = new Date(
                notification.createdAt
            ).toLocaleString();


            // Read / Unread status
            const status = document.createElement("small");

            status.className = notification.isRead
                ? "text-secondary"
                : "text-primary fw-bold";

            status.textContent = notification.isRead
                ? "Read"
                : "Unread";


            item.appendChild(message);
            item.appendChild(date);

            item.appendChild(document.createTextNode(" · "));

            item.appendChild(status);


            // View ticket link
            if (notification.ticketId) {

                const link = document.createElement("a");

                link.href = "/Ticket/Details/" +
                    encodeURIComponent(notification.ticketId);

                link.className =
                    "d-block small text-primary mt-1";

                link.textContent = "View Ticket";

                item.appendChild(link);

            }

            li.appendChild(item);

            list.appendChild(li);

        });

    }


    // View all notifications
    const footer = document.createElement("li");

    const viewAll = document.createElement("a");

    viewAll.href = "/Notification/Index";

    viewAll.className =
        "dropdown-item text-center text-primary fw-bold";

    viewAll.textContent = "View All Notifications";

    footer.appendChild(viewAll);

    list.appendChild(footer);

}


// ======================================
// 8. LOAD UNREAD NOTIFICATION COUNT
// ======================================

async function loadUnreadCount() {

    const countBadge = document.getElementById("notificationCount");

    if (!countBadge) {
        return;
    }

    try {

        const response = await fetch("/Notification/GetUnreadCount");

        if (!response.ok) {
            throw new Error("HTTP error: " + response.status);
        }

        const count = await response.json();

        console.log("Unread notification count:", count);

        countBadge.textContent = count;

        if (count > 0) {

            countBadge.style.display = "inline-block";

        }
        else {

            countBadge.style.display = "none";

        }

    }
    catch (error) {

        console.error("Unread count loading failed:", error);

    }

}


// ======================================
// 9. INITIALIZE NOTIFICATION DROPDOWN
// ======================================

document.addEventListener("DOMContentLoaded", function () {

    // Load notifications when page opens
    loadDropdownNotifications();

    // Load unread notification count
    loadUnreadCount();


    // Refresh whenever dropdown opens
    const notificationButton = document.getElementById(
        "notificationDropdown"
    );

    if (notificationButton) {

        notificationButton.addEventListener(
            "show.bs.dropdown",
            function () {

                loadDropdownNotifications();

            }
        );

    }

});


// ======================================
// 10. TEST SIGNALR TICKET UPDATE
// ======================================

async function testTicketUpdate() {

    try {

        if (connection.state !== signalR.HubConnectionState.Connected) {

            console.error("SignalR is not connected yet.");

            return;

        }

        await connection.invoke(
            "NotifyTicketStatus",
            10,
            "In Progress"
        );

        console.log("Ticket update event sent!");

    }
    catch (error) {

        console.error("Error sending ticket update:", error);

    }

}
