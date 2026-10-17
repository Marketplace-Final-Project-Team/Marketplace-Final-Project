(() => {
    const cards = Array.from(document.querySelectorAll("[data-wishlist-card]"));
    if (cards.length === 0) {
        return;
    }

    const STORAGE_KEY = "mp_wishlist_price_snapshot";

    const readSnapshot = () => {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            const value = raw ? JSON.parse(raw) : {};
            return value && typeof value === "object" ? value : {};
        } catch {
            return {};
        }
    };

    const writeSnapshot = (value) => {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
        } catch {
            // localStorage unavailable — price/stock alerts just won't persist across visits.
        }
    };

    const formatCurrency = (value) =>
        `${value.toLocaleString("uk-UA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₴`;

    const snapshot = readSnapshot();
    let alertCount = 0;

    cards.forEach((card) => {
        const productId = card.dataset.productId;
        const price = parseFloat(card.dataset.price);
        const inStock = card.dataset.inStock === "true";
        const previous = snapshot[productId];

        const alertEl = card.querySelector("[data-wishlist-alert]");
        const priceEl = card.querySelector("[data-wishlist-price]");

        if (previous) {
            if (Number.isFinite(previous.price) && price < previous.price) {
                if (alertEl) {
                    alertEl.hidden = false;
                    alertEl.textContent = __t("↓ Ціна знизилась");
                    alertEl.classList.add("wishlist-card__alert--price");
                }
                if (priceEl) {
                    if (!card.querySelector("[data-wishlist-original-price]")) {
                        priceEl.innerHTML = `<s class="wishlist-card__old-price">${formatCurrency(previous.price)}</s> ${formatCurrency(price)}`;
                    }
                }
                alertCount += 1;
            } else if (previous.inStock === false && inStock) {
                if (alertEl) {
                    alertEl.hidden = false;
                    alertEl.textContent = __t("Знову в наявності");
                    alertEl.classList.add("wishlist-card__alert--stock");
                }
                alertCount += 1;
            }
        }

        snapshot[productId] = { price, inStock };
    });

    writeSnapshot(snapshot);

    if (alertCount > 0) {
        const summary = document.querySelector(".profile-summary p");
        if (summary) {
            const note = document.createElement("p");
            note.className = "wishlist-notifications-summary";
            note.textContent =
                alertCount === 1
                    ? __t("В 1 товарі щось змінилося з часу останнього перегляду - дивіться позначку на картці.")
                    : __t("У {0} товарах щось змінилося з часу останнього перегляду - дивіться позначки на картках.", alertCount);
            summary.insertAdjacentElement("afterend", note);
        }
    }
})();
