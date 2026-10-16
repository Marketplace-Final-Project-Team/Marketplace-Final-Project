(() => {
    const section = document.querySelector("[data-cart-recommendations]");
    const grid = section?.querySelector("[data-cart-recommendations-grid]");

    if (!section || !grid) return;

    const formatMoney = (value) => new Intl.NumberFormat("uk-UA", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(Number(value || 0));

    const productUrl = (id) => `/Catalog/Details/${encodeURIComponent(id)}`;

    function createCard(product) {
        const card = document.createElement("article");
        card.className = "cart-recommendation-card";

        const media = document.createElement("a");
        media.className = "cart-recommendation-card__media";
        media.href = productUrl(product.productId);
        media.setAttribute("aria-label", product.productName);

        if (product.productImageUrl) {
            const image = document.createElement("img");
            image.src = product.productImageUrl;
            image.alt = product.productName;
            image.loading = "lazy";
            media.appendChild(image);
        } else {
            media.textContent = (product.productName || "M").slice(0, 1).toUpperCase();
        }

        if (product.isAdultOnly) {
            const badge = document.createElement("span");
            badge.className = "cart-recommendation-card__badge";
            badge.textContent = "18+";
            media.appendChild(badge);
        }

        const body = document.createElement("div");
        body.className = "cart-recommendation-card__body";

        const category = document.createElement("span");
        category.className = "cart-recommendation-card__category";
        category.textContent = product.categoryName || "Marketplace";

        const name = document.createElement("a");
        name.className = "cart-recommendation-card__name";
        name.href = productUrl(product.productId);
        name.textContent = product.productName;

        const price = document.createElement("div");
        price.className = "cart-recommendation-card__price";
        const finalPrice = Number(product.finalPrice || product.price);
        const originalPrice = Number(product.price);
        price.innerHTML = `<strong>${formatMoney(finalPrice)} ₴</strong>`;
        if (originalPrice > finalPrice) {
            const oldPrice = document.createElement("s");
            oldPrice.textContent = `${formatMoney(originalPrice)} ₴`;
            price.appendChild(oldPrice);
        }

        const action = document.createElement("a");
        action.className = "cart-recommendation-card__action";
        action.href = productUrl(product.productId);
        action.textContent = "Переглянути товар";

        body.append(category, name, price, action);
        card.append(media, body);
        return card;
    }

    fetch(section.dataset.endpoint, { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then((response) => response.ok ? response.json() : [])
        .then((items) => {
            const availableItems = Array.isArray(items)
                ? items.filter((item) => Number(item.quantity) > 0).slice(0, 4)
                : [];

            if (availableItems.length === 0) return;

            availableItems.forEach((item) => grid.appendChild(createCard(item)));
            section.hidden = false;
        })
        .catch(() => {
            section.hidden = true;
        });
})();
