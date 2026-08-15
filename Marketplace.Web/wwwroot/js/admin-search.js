(() => {
    document.querySelectorAll("[data-admin-search]").forEach((input) => {
        const table = document.querySelector(input.dataset.adminSearch);
        if (!table) {
            return;
        }

        const rows = Array.from(table.querySelectorAll("tbody tr"));
        const emptyState = document.querySelector(input.dataset.adminSearchEmpty || "[data-admin-search-empty]");
        const panel = table.closest(".admin-panel");

        input.addEventListener("input", () => {
            const query = input.value.trim().toLowerCase();
            let visibleCount = 0;

            rows.forEach((row) => {
                const matches = row.textContent.toLowerCase().includes(query);
                row.hidden = !matches;
                if (matches) {
                    visibleCount += 1;
                }
            });

            if (panel) {
                panel.hidden = visibleCount === 0;
            }
            if (emptyState) {
                emptyState.hidden = visibleCount !== 0;
            }
        });
    });
})();
