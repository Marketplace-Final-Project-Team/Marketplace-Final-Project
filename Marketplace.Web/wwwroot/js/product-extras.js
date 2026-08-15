(() => {
    // ---------- Variant swatches/chips (visual selection only) ----------
    document.querySelectorAll("[data-variant-group]").forEach((group) => {
        group.querySelectorAll("[data-variant-option]").forEach((option) => {
            option.addEventListener("click", () => {
                group.querySelectorAll("[data-variant-option]").forEach((o) => {
                    o.classList.remove("is-selected");
                    o.setAttribute("aria-pressed", "false");
                });
                option.classList.add("is-selected");
                option.setAttribute("aria-pressed", "true");
            });
        });
    });

    // ---------- Size guide modal ----------
    const sizeGuide = document.querySelector("[data-size-guide]");
    if (sizeGuide) {
        document.querySelectorAll("[data-open-size-guide]").forEach((btn) => {
            btn.addEventListener("click", () => {
                sizeGuide.hidden = false;
            });
        });

        sizeGuide.querySelectorAll("[data-close-size-guide]").forEach((btn) => {
            btn.addEventListener("click", () => {
                sizeGuide.hidden = true;
            });
        });

        sizeGuide.addEventListener("click", (event) => {
            if (event.target === sizeGuide) {
                sizeGuide.hidden = true;
            }
        });
    }

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
