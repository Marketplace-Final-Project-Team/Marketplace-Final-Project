(() => {
    function debounce(fn, delay) {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn(...args), delay);
        };
    }

    function escapeHtml(value) {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    }

    function formatPrice(price) {
        return `${Number(price).toLocaleString("uk-UA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₴`;
    }

    document.querySelectorAll("[data-search-autocomplete]").forEach((input) => {
        const wrapper = input.closest("[data-search-wrapper]") || input.parentElement;
        if (!wrapper) return;

        const dropdown = document.createElement("div");
        dropdown.className = "search-suggest";
        dropdown.hidden = true;
        wrapper.appendChild(dropdown);

        let activeIndex = -1;

        const closeDropdown = () => {
            dropdown.hidden = true;
            dropdown.innerHTML = "";
            activeIndex = -1;
        };

        const submitSearch = (term) => {
            const url = new URL("/Catalog", window.location.origin);
            if (term) url.searchParams.set("Search", term);
            window.location.href = url.toString();
        };

        const renderResults = (data) => {
            const items = data.items || data.Items || [];
            const term = data.term || data.Term || "";
            const corrected = data.correctedTerm || data.CorrectedTerm;

            if (items.length === 0) {
                closeDropdown();
                return;
            }

            let html = "";
            if (corrected && corrected.toLowerCase() !== term.toLowerCase()) {
                html += `<button type="button" class="search-suggest__corrected" data-corrected-term="${escapeHtml(corrected)}">${__t("Можливо, ви мали на увазі «{0}»?", `<strong>${escapeHtml(corrected)}</strong>`)}</button>`;
            }

            html += items
                .map((item, index) => {
                    const id = item.id || item.Id;
                    const name = item.name || item.Name;
                    const category = item.categoryName || item.CategoryName;
                    const price = item.price ?? item.Price;
                    const imageUrl = item.imageUrl || item.ImageUrl;
                    const media = imageUrl
                        ? `<img src="${escapeHtml(imageUrl)}" alt="" />`
                        : `<span>${escapeHtml((name || "?").charAt(0).toUpperCase())}</span>`;

                    return `
                        <a href="/Catalog/Details/${encodeURIComponent(id)}" class="search-suggest__item" data-suggest-index="${index}">
                            <span class="search-suggest__media">${media}</span>
                            <span class="search-suggest__body">
                                <span class="search-suggest__name">${escapeHtml(name)}</span>
                                <span class="search-suggest__meta">${escapeHtml(category)} · ${formatPrice(price)}</span>
                            </span>
                        </a>
                    `;
                })
                .join("");

            html += `<button type="button" class="search-suggest__all" data-show-all>${__t("Усі результати для «{0}» →", escapeHtml(term))}</button>`;

            dropdown.innerHTML = html;
            dropdown.hidden = false;
            activeIndex = -1;

            dropdown.querySelector("[data-corrected-term]")?.addEventListener("click", (event) => {
                const newTerm = event.currentTarget.dataset.correctedTerm;
                input.value = newTerm;
                fetchSuggestions(newTerm);
                input.focus();
            });

            dropdown.querySelector("[data-show-all]")?.addEventListener("click", () => submitSearch(term));
        };

        const fetchSuggestions = debounce((term) => {
            if (!term || term.trim().length < 2) {
                closeDropdown();
                return;
            }

            fetch(`/Catalog/SearchSuggestions?term=${encodeURIComponent(term.trim())}`)
                .then((response) => (response.ok ? response.json() : null))
                .then((data) => {
                    if (data) renderResults(data);
                })
                .catch(() => closeDropdown());
        }, 250);

        input.addEventListener("input", () => fetchSuggestions(input.value));

        input.addEventListener("keydown", (event) => {
            const links = Array.from(dropdown.querySelectorAll(".search-suggest__item"));
            if (dropdown.hidden || links.length === 0) return;

            if (event.key === "ArrowDown") {
                event.preventDefault();
                activeIndex = Math.min(activeIndex + 1, links.length - 1);
                links.forEach((link, i) => link.classList.toggle("is-active", i === activeIndex));
                links[activeIndex]?.scrollIntoView({ block: "nearest" });
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                activeIndex = Math.max(activeIndex - 1, 0);
                links.forEach((link, i) => link.classList.toggle("is-active", i === activeIndex));
                links[activeIndex]?.scrollIntoView({ block: "nearest" });
            } else if (event.key === "Enter" && activeIndex >= 0) {
                event.preventDefault();
                links[activeIndex].click();
            } else if (event.key === "Escape") {
                closeDropdown();
            }
        });

        document.addEventListener("click", (event) => {
            if (!wrapper.contains(event.target)) {
                closeDropdown();
            }
        });
    });
})();
