(() => {
    const listEl = document.querySelector("[data-saved-searches-list]");
    const sectionEl = document.querySelector("[data-saved-searches]");
    const saveButton = document.querySelector("[data-save-search]");
    if (!listEl || !sectionEl) {
        return;
    }

    const STORAGE_KEY = "mp_saved_searches";

    const readSaved = () => {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            const items = raw ? JSON.parse(raw) : [];
            return Array.isArray(items) ? items : [];
        } catch {
            return [];
        }
    };

    const writeSaved = (items) => {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        } catch {
            // localStorage unavailable — saved searches just won't persist across reloads.
        }
    };

    const escapeHtml = (value) => {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    };

    const currentProductIds = () =>
        Array.from(document.querySelectorAll("[data-product-id]")).map((el) => el.dataset.productId);

    const buildSummary = () => {
        const params = new URLSearchParams(window.location.search);
        const parts = [];

        const search = params.get("Search");
        if (search) parts.push(`«${search}»`);

        const categorySelect = document.getElementById("filter-category");
        const categoryId = params.get("CategoryId");
        if (categoryId && categorySelect) {
            const option = Array.from(categorySelect.options).find((o) => o.value === categoryId);
            if (option) parts.push(option.textContent.trim());
        }

        const minPrice = params.get("MinPrice");
        const maxPrice = params.get("MaxPrice");
        if (minPrice || maxPrice) parts.push(`${minPrice || "0"}–${maxPrice || "…"} ₴`);

        const minRating = params.get("MinRating");
        if (minRating) parts.push(`${minRating}★+`);

        if (params.get("InStockOnly")) parts.push("в наявності");

        return parts.length > 0 ? parts.join(" · ") : "Усі товари";
    };

    const render = () => {
        const saved = readSaved();

        if (saved.length === 0) {
            sectionEl.hidden = true;
            return;
        }

        sectionEl.hidden = false;
        const currentQuery = window.location.search.replace(/^\?/, "");
        const ids = currentProductIds();
        let mutated = false;

        listEl.innerHTML = saved
            .map((entry) => {
                const isActive = entry.query === currentQuery;
                let newCount = 0;

                if (isActive && ids.length > 0) {
                    const previousIds = new Set(entry.productIds || []);
                    const newIds = ids.filter((id) => !previousIds.has(id));
                    newCount = newIds.length;

                    newIds.forEach((id) => {
                        const card = document.querySelector(`[data-product-id="${id}"]`);
                        card?.querySelector("[data-new-badge]")?.removeAttribute("hidden");
                    });

                    if (newCount > 0 || previousIds.size === 0) {
                        entry.productIds = ids;
                        entry.updatedAt = new Date().toISOString();
                        mutated = true;
                    }
                }

                return `
                    <div class="saved-search-card ${isActive ? "is-active" : ""}">
                        <a href="/Catalog?${escapeHtml(entry.query)}" class="saved-search-card__link">
                            <strong>${escapeHtml(entry.name)}</strong>
                            <span>${escapeHtml(entry.summary)}</span>
                        </a>
                        ${newCount > 0 ? `<span class="saved-search-card__badge">+${newCount} нових</span>` : ""}
                        <button type="button" class="saved-search-card__remove" data-remove-search="${escapeHtml(entry.id)}" aria-label="Видалити збережений пошук">×</button>
                    </div>
                `;
            })
            .join("");

        if (mutated) {
            writeSaved(saved);
        }

        listEl.querySelectorAll("[data-remove-search]").forEach((button) => {
            button.addEventListener("click", () => {
                const remaining = readSaved().filter((entry) => entry.id !== button.dataset.removeSearch);
                writeSaved(remaining);
                render();
            });
        });
    };

    saveButton?.addEventListener("click", () => {
        const defaultName = buildSummary();
        const name = window.prompt("Назва для збереженого пошуку:", defaultName === "Усі товари" ? "Мій пошук" : defaultName);
        if (!name || !name.trim()) return;

        const saved = readSaved();
        saved.push({
            id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
            name: name.trim(),
            query: window.location.search.replace(/^\?/, ""),
            summary: buildSummary(),
            productIds: currentProductIds(),
            updatedAt: new Date().toISOString(),
        });
        writeSaved(saved);
        render();
    });

    render();
})();
