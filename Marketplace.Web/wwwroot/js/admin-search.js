(() => {
    const tables = new Map();

    const getState = (selector) => {
        if (!selector) return null;
        if (!tables.has(selector)) {
            const table = document.querySelector(selector);
            if (!table) return null;
            tables.set(selector, {
                table,
                rows: Array.from(table.querySelectorAll("tbody tr")),
                searchInput: null,
                statusSelect: null,
                sortSelect: null,
                emptyState: null,
                panel: table.closest(".admin-panel")
            });
        }
        return tables.get(selector);
    };

    document.querySelectorAll("[data-admin-search]").forEach((input) => {
        const state = getState(input.dataset.adminSearch);
        if (!state) return;
        state.searchInput = input;
        state.emptyState = document.querySelector(input.dataset.adminSearchEmpty || "[data-admin-search-empty]");
    });

    document.querySelectorAll("[data-admin-status-filter]").forEach((select) => {
        const state = getState(select.dataset.adminStatusFilter);
        if (state) state.statusSelect = select;
    });

    document.querySelectorAll("[data-admin-sort]").forEach((select) => {
        const state = getState(select.dataset.adminSort);
        if (state) state.sortSelect = select;
    });

    tables.forEach((state) => {
        const applyFilters = () => {
            const query = state.searchInput ? state.searchInput.value.trim().toLowerCase() : "";
            const status = state.statusSelect ? state.statusSelect.value : "";
            let visibleCount = 0;

            state.rows.forEach((row) => {
                const matchesText = !query || row.textContent.toLowerCase().includes(query);
                const matchesStatus = !status || row.dataset.status === status;
                const visible = matchesText && matchesStatus;
                row.hidden = !visible;
                if (visible) {
                    visibleCount += 1;
                }
            });

            if (state.panel) {
                state.panel.hidden = visibleCount === 0;
            }
            if (state.emptyState) {
                state.emptyState.hidden = visibleCount !== 0;
            }
        };

        const applySort = () => {
            if (!state.sortSelect || !state.sortSelect.value) {
                return;
            }

            const [key, dir] = state.sortSelect.value.split("-");
            const dataKey = "sort" + key.charAt(0).toUpperCase() + key.slice(1);
            const tbody = state.table.querySelector("tbody");

            const sorted = [...state.rows].sort((a, b) => {
                const av = a.dataset[dataKey] ?? "";
                const bv = b.dataset[dataKey] ?? "";
                const an = parseFloat(av);
                const bn = parseFloat(bv);
                const bothNumeric = av !== "" && bv !== "" && !Number.isNaN(an) && !Number.isNaN(bn);
                const cmp = bothNumeric ? an - bn : av.localeCompare(bv, "uk");
                return dir === "desc" ? -cmp : cmp;
            });

            sorted.forEach((row) => tbody.appendChild(row));
        };

        if (state.searchInput) {
            state.searchInput.addEventListener("input", applyFilters);
        }
        if (state.statusSelect) {
            state.statusSelect.addEventListener("change", applyFilters);
        }
        if (state.sortSelect) {
            state.sortSelect.addEventListener("change", applySort);
        }
    });
})();
