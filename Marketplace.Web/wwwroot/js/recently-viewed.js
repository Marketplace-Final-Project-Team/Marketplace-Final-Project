(() => {
    const STORAGE_KEY = "mp_recently_viewed";
    const MAX_ITEMS = 8;

    function readItems() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            const items = raw ? JSON.parse(raw) : [];
            return Array.isArray(items) ? items : [];
        } catch {
            return [];
        }
    }

    function writeItems(items) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        } catch {
            // localStorage unavailable (private mode / quota) — recently-viewed is a nice-to-have,
            // so just skip persisting instead of breaking the page.
        }
    }

    function escapeHtml(value) {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    }

    function currentProduct() {
        const dataEl = document.getElementById("current-product-data");
        if (!dataEl) {
            return null;
        }
        try {
            const product = JSON.parse(dataEl.textContent);
            return product && product.id ? product : null;
        } catch {
            return null;
        }
    }

    function recordView() {
        const product = currentProduct();
        if (!product) {
            return;
        }

        const items = readItems().filter((item) => item.id !== product.id);
        items.unshift(product);
        writeItems(items.slice(0, MAX_ITEMS));
    }

    function renderList() {
        const container = document.querySelector("[data-recently-viewed-list]");
        if (!container) {
            return;
        }

        const excludeId = currentProduct()?.id ?? null;
        const items = readItems().filter((item) => item.id !== excludeId);
        if (items.length === 0) {
            return;
        }

        const colors = ["blue", "violet", "orange"];

        container.innerHTML = items
            .map((item, index) => {
                const color = colors[index % colors.length];
                const name = escapeHtml(item.name);
                const category = escapeHtml(item.categoryName || "");
                const media = item.imageUrl
                    ? `<img src="${escapeHtml(item.imageUrl)}" alt="${name}" />`
                    : `<span>${escapeHtml((item.name || "?").charAt(0).toUpperCase())}</span>`;

                return `
                    <article class="home-product">
                        <a href="/Catalog/Details/${encodeURIComponent(item.id)}" class="home-product__media home-product__media--${color}">
                            ${media}
                            <small>Переглянуто</small>
                        </a>
                        <div class="home-product__body">
                            <div class="home-product__category">${category}</div>
                            <a href="/Catalog/Details/${encodeURIComponent(item.id)}" class="home-product__name">${name}</a>
                            <div class="home-product__bottom">
                                <strong>${escapeHtml(item.price)}</strong>
                            </div>
                        </div>
                    </article>
                `;
            })
            .join("");

        const section = container.closest("[data-recently-viewed-section]");
        if (section) {
            section.hidden = false;
        }
    }

    recordView();
    renderList();
})();
