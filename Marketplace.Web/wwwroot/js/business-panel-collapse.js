(() => {
    document.querySelectorAll(".business-panel[data-collapsible]").forEach((panel) => {
        const btn = panel.querySelector("[data-collapse-toggle]");
        if (!btn) {
            return;
        }

        btn.addEventListener("click", () => {
            const collapsed = panel.classList.toggle("is-collapsed");
            btn.setAttribute("aria-expanded", String(!collapsed));
        });
    });
})();
