(() => {
    // ---------- Product Q&A (stored locally per-browser, no backend) ----------
    const qaSection = document.querySelector("[data-qa-section]");
    if (!qaSection) {
        return;
    }

    const productId = qaSection.dataset.productId;
    const storageKey = `mp_product_questions_${productId}`;
    const listEl = qaSection.querySelector("[data-qa-list]");
    const emptyEl = qaSection.querySelector("[data-qa-empty]");
    const input = qaSection.querySelector("[data-qa-input]");
    const submitButton = qaSection.querySelector("[data-qa-submit]");

    const escapeHtml = (value) => {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    };

    const readQuestions = () => {
        try {
            const raw = localStorage.getItem(storageKey);
            const items = raw ? JSON.parse(raw) : [];
            return Array.isArray(items) ? items : [];
        } catch {
            return [];
        }
    };

    const writeQuestions = (items) => {
        try {
            localStorage.setItem(storageKey, JSON.stringify(items));
        } catch {
            // localStorage unavailable — question stays visible for this page view only.
        }
    };

    const renderQuestions = () => {
        const items = readQuestions();
        if (items.length === 0) {
            if (emptyEl) emptyEl.hidden = false;
            listEl.querySelectorAll("[data-qa-item]").forEach((el) => el.remove());
            return;
        }

        if (emptyEl) emptyEl.hidden = true;
        listEl.querySelectorAll("[data-qa-item]").forEach((el) => el.remove());

        items
            .slice()
            .reverse()
            .forEach((item) => {
                const wrapper = document.createElement("div");
                wrapper.className = "product-qa__item";
                wrapper.dataset.qaItem = "true";
                wrapper.innerHTML = `
                    <p>${escapeHtml(item.text)}</p>
                    <span class="product-qa__status">Очікує відповіді продавця · ${escapeHtml(item.date)}</span>
                `;
                listEl.appendChild(wrapper);
            });
    };

    submitButton?.addEventListener("click", () => {
        const text = input.value.trim();
        if (!text) {
            input.focus();
            return;
        }

        const items = readQuestions();
        items.push({ text, date: new Date().toLocaleDateString("uk-UA") });
        writeQuestions(items);
        input.value = "";
        renderQuestions();
    });

    renderQuestions();
})();
