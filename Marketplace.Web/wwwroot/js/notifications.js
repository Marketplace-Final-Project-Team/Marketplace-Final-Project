(() => {
    const buttons = Array.from(document.querySelectorAll("[data-notif-tab]"));
    const panels = Array.from(document.querySelectorAll("[data-notif-panel]"));

    if (buttons.length === 0 || panels.length === 0) {
        return;
    }

    const allFeed = document.querySelector("[data-notif-all-feed]");
    const messageFeed = document.querySelector("[data-notif-message-feed]");
    const messageCount = document.querySelector("[data-message-count]");
    const allEmpty = document.querySelector("[data-notif-all-empty]");
    const messageEmpty = document.querySelector("[data-notif-message-empty]");

    function activateTab(target) {
        buttons.forEach((button) => {
            const isActive = button.dataset.notifTab === target;
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-selected", isActive ? "true" : "false");
        });

        panels.forEach((panel) => {
            panel.hidden = panel.dataset.notifPanel !== target;
        });
    }

    buttons.forEach((button) => {
        button.addEventListener("click", () => activateTab(button.dataset.notifTab));
    });

    document.querySelectorAll("[data-open-notif-tab]").forEach((button) => {
        button.addEventListener("click", () => activateTab(button.dataset.openNotifTab));
    });

    function notificationGroup(type) {
        if (type === "RefundChat" || type === "SupportTicket") {
            return "message";
        }

        return "order";
    }

    function notificationLabel(type) {
        if (type === "RefundChat") return "Чат повернення";
        if (type === "SupportTicket") return "Підтримка";
        return "Замовлення";
    }

    function createFeedItem(item) {
        const group = notificationGroup(item.type);
        const link = document.createElement("a");
        link.className = `notif-feed-item notif-feed-item--${group}`;
        link.href = item.url || "/Profile/Activity";
        link.dataset.notifFeedEntry = "";
        link.dataset.time = item.time || "";

        const icon = document.createElement("span");
        icon.className = "notif-feed-item__icon";
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = group === "message" ? "↗" : "✓";

        const content = document.createElement("span");
        content.className = "notif-feed-item__content";

        const type = document.createElement("span");
        type.className = "notif-feed-item__type";
        type.textContent = notificationLabel(item.type);

        const text = document.createElement("strong");
        text.textContent = item.text || "Нове сповіщення";

        const time = document.createElement("time");
        const parsedTime = new Date(item.time);
        time.textContent = Number.isNaN(parsedTime.getTime())
            ? ""
            : parsedTime.toLocaleString("uk-UA", {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit"
            });

        content.append(type, text);
        link.append(icon, content, time);
        return link;
    }

    function sortFeed(feed) {
        if (!feed) return;

        const entries = Array.from(feed.querySelectorAll("[data-notif-feed-entry]"));
        entries
            .sort((left, right) => {
                const leftTime = Date.parse(left.dataset.time || "") || 0;
                const rightTime = Date.parse(right.dataset.time || "") || 0;
                return rightTime - leftTime;
            })
            .forEach((entry) => feed.appendChild(entry));
    }

    async function loadActivityNotifications() {
        if (!allFeed || !messageFeed) return;

        try {
            const response = await fetch("/Notification/GetRecent", {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const items = await response.json();
            const messages = items.filter((item) => notificationGroup(item.type) === "message");

            messageFeed.replaceChildren(...messages.map(createFeedItem));
            items.forEach((item) => allFeed.appendChild(createFeedItem(item)));
            sortFeed(allFeed);

            if (messageCount) {
                messageCount.textContent = messages.length;
                messageCount.hidden = messages.length === 0;
            }

            if (messageEmpty) messageEmpty.hidden = messages.length !== 0;
            if (allEmpty) allEmpty.hidden = allFeed.querySelector("[data-notif-feed-entry]") !== null;
        } catch (error) {
            console.error("Помилка завантаження стрічки сповіщень:", error);
            messageFeed.replaceChildren();
            if (messageEmpty) messageEmpty.hidden = false;
            if (allEmpty) allEmpty.hidden = allFeed.querySelector("[data-notif-feed-entry]") !== null;
        }
    }

    loadActivityNotifications();
})();



(() => {
    const bellButton = document.querySelector("[data-notif-bell]");
    if (!bellButton) {
        return;
    }

    const badge = document.querySelector("[data-notif-badge]");
    const dropdown = document.querySelector("[data-notif-dropdown]");
    const dropdownList = document.querySelector("[data-notif-list]");

    let toastContainer = document.querySelector(".notif-toast-container");
    if (!toastContainer) {
        toastContainer = document.createElement("div");
        toastContainer.className = "notif-toast-container";
        document.body.appendChild(toastContainer);
    }

    function updateBadge(count) {
        if (!badge) return;
        if (count > 0) {
            badge.textContent = count > 99 ? "99+" : count;
            badge.hidden = false;
        } else {
            badge.hidden = true;
        }
    }

    async function refreshCount() {
        try {
            const response = await fetch("/Notification/GetUnreadCount");
            if (!response.ok) return;
            const data = await response.json();
            updateBadge(data.count);
        } catch (error) {
            console.error("Помилка отримання кількості сповіщень:", error);
        }
    }

    async function loadRecent() {
        if (!dropdownList) return;
        dropdownList.innerHTML = '<div class="notif-dropdown__empty">Завантаження...</div>';

        try {
            const response = await fetch("/Notification/GetRecent");
            if (!response.ok) return;
            const items = await response.json();

            if (items.length === 0) {
                dropdownList.innerHTML = '<div class="notif-dropdown__empty">Немає нових сповіщень</div>';
                return;
            }

            dropdownList.innerHTML = items.map(item => {
                const time = new Date(item.time).toLocaleString("uk-UA", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
                return `
                    <a href="${item.url}" class="notif-item">
                        <div class="notif-item__text">${escapeHtml(item.text)}</div>
                        <div class="notif-item__time">${time}</div>
                    </a>
                `;
            }).join("");
        } catch (error) {
            console.error("Помилка завантаження сповіщень:", error);
        }
    }

    function escapeHtml(text) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    bellButton.addEventListener("click", () => {
        const isHidden = dropdown.hidden;
        dropdown.hidden = !isHidden;
        if (isHidden) {
            loadRecent();
        }
    });

    document.addEventListener("click", (event) => {
        if (!bellButton.contains(event.target) && !dropdown.contains(event.target)) {
            dropdown.hidden = true;
        }
    });

    function showToast(text) {
        const toast = document.createElement("div");
        toast.className = "notif-toast";
        toast.textContent = text;

        toast.addEventListener("click", () => {
            dismissToast(toast);
            bellButton.click();
        });

        toastContainer.appendChild(toast);

        setTimeout(() => dismissToast(toast), 6000);
    }

    function dismissToast(toast) {
        if (!toast.isConnected) return;
        toast.classList.add("notif-toast--out");
        setTimeout(() => toast.remove(), 250);
    }


    if (typeof signalR !== "undefined") {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", (data) => {
            showToast(data.text);
            refreshCount();
        });

        connection.start().catch(err => console.error("NotificationHub помилка з'єднання:", err));
    }

    refreshCount();
})();
