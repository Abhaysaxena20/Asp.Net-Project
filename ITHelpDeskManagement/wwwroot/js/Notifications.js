let allNotifications = [];
let currentFilter = "all";

document.addEventListener("DOMContentLoaded", function () {
    loadNotifications();

    document.querySelectorAll(".filter-btn").forEach(button => {
        button.addEventListener("click", function () {
            currentFilter = this.dataset.filter;
            renderNotifications();

            document.querySelectorAll(".filter-btn").forEach(btn => {
                btn.classList.remove("btn-primary");
                btn.classList.add("btn-outline-primary");
            });

            this.classList.remove("btn-outline-primary");
            this.classList.add("btn-primary");
        });
    });
});

async function loadNotifications() {
    const list = document.getElementById("notificationPageList");

    if (!list) {
        console.error("notificationPageList element not found!");
        return;
    }

    try {
        const response = await fetch(
            "/Notification/GetMyNotifications",
            {
                credentials: "same-origin"
            }
        );

        console.log("Response Status:", response.status);
        console.log("Response URL:", response.url);

        if (!response.ok) {
            throw new Error("HTTP error: " + response.status);
        }

        allNotifications = await response.json();

        console.log("Notifications received:", allNotifications);
        console.log("Total notifications:", allNotifications.length);

        renderNotifications();

    }
    catch (error) {
        console.error("Notification loading failed:", error);

        list.textContent = "Unable to load notifications.";
    }
}

function renderNotifications() {
    const list = document.getElementById("notificationPageList");

    list.replaceChildren();

    let filtered = allNotifications;

    if (currentFilter === "unread") {
        filtered = allNotifications.filter(n => !n.isRead);
    }
    else if (currentFilter === "read") {
        filtered = allNotifications.filter(n => n.isRead);
    }

    if (filtered.length === 0) {
        const message = document.createElement("p");
        message.className = "text-center text-muted py-4";
        message.textContent = "No notifications found.";
        list.appendChild(message);
        return;
    }

    filtered.forEach(notification => {
        const item = document.createElement("div");
        item.className = "border-bottom p-3";

        const message = document.createElement("p");
        message.className = "mb-1";
        message.textContent = notification.message;

        const date = document.createElement("small");
        date.className = "text-muted";
        date.textContent = new Date(notification.createdAt).toLocaleString();

        const status = document.createElement("span");
        status.className = notification.isRead
            ? "badge bg-secondary ms-2"
            : "badge bg-primary ms-2";
        status.textContent = notification.isRead ? "Read" : "Unread";

        item.appendChild(message);
        item.appendChild(date);
        item.appendChild(status);

        if (notification.ticketId) {
            const link = document.createElement("a");
            link.className = "btn btn-sm btn-outline-primary ms-3";
            link.href = `/Ticket/Details/${encodeURIComponent(notification.ticketId)}`;
            link.textContent = "View Ticket";
            item.appendChild(link);
        }

        list.appendChild(item);
    });
}