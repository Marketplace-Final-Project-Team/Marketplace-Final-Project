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
