(() => {
    const buttons = Array.from(document.querySelectorAll("[data-notif-tab]"));
    const panels = Array.from(document.querySelectorAll("[data-notif-panel]"));

    if (buttons.length === 0 || panels.length === 0) {
        return;
    }

    buttons.forEach((button) => {
        button.addEventListener("click", () => {
            const target = button.dataset.notifTab;

            buttons.forEach((b) => {
                const isActive = b === button;
                b.classList.toggle("is-active", isActive);
                b.setAttribute("aria-selected", isActive ? "true" : "false");
            });

            panels.forEach((panel) => {
                panel.hidden = panel.dataset.notifPanel !== target;
            });
        });
    });
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