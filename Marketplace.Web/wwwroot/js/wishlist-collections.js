(() => {
    const grid = document.querySelector("[data-wishlist-grid]");
    if (!grid) {
        return;
    }

    const NAMES_KEY = "mp_wishlist_collection_names";
    const ASSIGNMENTS_KEY = "mp_wishlist_collection_assignments";
    const COLLAPSED_KEY = "mp_wishlist_collapsed_collections";
    const UNASSIGNED = "Без колекції";

    const readJson = (key, fallback) => {
        try {
            const raw = localStorage.getItem(key);
            const value = raw ? JSON.parse(raw) : fallback;
            return value ?? fallback;
        } catch {
            return fallback;
        }
    };

    const writeJson = (key, value) => {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch {
            // localStorage unavailable — collections just won't persist across reloads.
        }
    };

    const getCollectionNames = () => readJson(NAMES_KEY, []);
    const getAssignments = () => readJson(ASSIGNMENTS_KEY, {});
    const getCollapsed = () => new Set(readJson(COLLAPSED_KEY, []));
    const writeCollapsed = (set) => writeJson(COLLAPSED_KEY, Array.from(set));

    const cards = Array.from(grid.querySelectorAll("[data-wishlist-card]"));
    const cardsContainer = grid.parentElement;

    const render = () => {
        const names = getCollectionNames();
        const assignments = getAssignments();
        const allNames = [UNASSIGNED, ...names];

        // Group cards by their assigned collection.
        const groups = new Map(allNames.map((name) => [name, []]));
        cards.forEach((card) => {
            const productId = card.dataset.productId;
            const collection = assignments[productId] && groups.has(assignments[productId])
                ? assignments[productId]
                : UNASSIGNED;
            groups.get(collection).push(card);
        });

        cardsContainer.querySelectorAll("[data-collection-group]").forEach((el) => el.remove());

        if (names.length === 0) {
            // No custom collections yet — keep the plain, ungrouped grid.
            grid.hidden = false;
            grid.append(...cards);
            cards.forEach((card) => {
                const select = card.querySelector("[data-collection-select]");
                if (select) {
                    select.innerHTML = `<option value="${UNASSIGNED}" selected>${UNASSIGNED}</option>`;
                }
            });
            return;
        }

        grid.hidden = true;

        const collapsed = getCollapsed();

        allNames.forEach((name) => {
            const items = groups.get(name);
            if (items.length === 0) {
                return;
            }

            const isCollapsed = collapsed.has(name);

            const section = document.createElement("div");
            section.className = "wishlist-collection-group" + (isCollapsed ? " is-collapsed" : "");
            section.dataset.collectionGroup = "true";

            const heading = document.createElement("button");
            heading.type = "button";
            heading.className = "wishlist-collection-group__heading";
            heading.setAttribute("aria-expanded", String(!isCollapsed));
            heading.innerHTML = `
                <svg class="wishlist-collection-group__chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16" aria-hidden="true">
                    <path d="m9 18 6-6-6-6" />
                </svg>
                <h3>${name} (${items.length})</h3>
            `;
            section.appendChild(heading);

            const list = document.createElement("div");
            list.className = "wishlist-grid";
            list.hidden = isCollapsed;
            items.forEach((card) => list.appendChild(card));
            section.appendChild(list);

            heading.addEventListener("click", () => {
                const nowCollapsed = section.classList.toggle("is-collapsed");
                list.hidden = nowCollapsed;
                heading.setAttribute("aria-expanded", String(!nowCollapsed));

                const stored = getCollapsed();
                if (nowCollapsed) {
                    stored.add(name);
                } else {
                    stored.delete(name);
                }
                writeCollapsed(stored);
            });

            cardsContainer.insertBefore(section, grid);
        });

        cards.forEach((card) => {
            const select = card.querySelector("[data-collection-select]");
            if (!select) return;

            const productId = card.dataset.productId;
            const current = assignments[productId] && allNames.includes(assignments[productId])
                ? assignments[productId]
                : UNASSIGNED;

            select.innerHTML = allNames
                .map((name) => `<option value="${name}" ${name === current ? "selected" : ""}>${name}</option>`)
                .join("");
        });
    };

    cards.forEach((card) => {
        const select = card.querySelector("[data-collection-select]");
        select?.addEventListener("change", () => {
            const assignments = getAssignments();
            assignments[card.dataset.productId] = select.value;
            writeJson(ASSIGNMENTS_KEY, assignments);
            render();
        });
    });

    const addButton = document.querySelector("[data-add-collection]");
    const nameInput = document.querySelector("[data-new-collection-input]");

    addButton?.addEventListener("click", () => {
        const value = nameInput.value.trim();
        if (!value) {
            nameInput.focus();
            return;
        }

        const names = getCollectionNames();
        if (!names.includes(value) && value !== UNASSIGNED) {
            names.push(value);
            writeJson(NAMES_KEY, names);
        }
        nameInput.value = "";
        render();
    });

    render();
})();
