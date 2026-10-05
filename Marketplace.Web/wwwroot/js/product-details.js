(() => {
    const mainImage = document.querySelector("[data-gallery-main]");
    const counter = document.querySelector("[data-gallery-counter]");
    const thumbnails = document.querySelectorAll("[data-gallery-thumb]");

    if (!mainImage || thumbnails.length === 0) {
        return;
    }

    thumbnails.forEach((thumbnail) => {
        thumbnail.addEventListener("click", () => {
            const imageUrl = thumbnail.dataset.imageUrl;
            const imageAlt = thumbnail.dataset.imageAlt;
            const imageIndex = thumbnail.dataset.imageIndex;

            if (!imageUrl) {
                return;
            }

            mainImage.src = imageUrl;
            mainImage.alt = imageAlt || mainImage.alt;
            mainImage.parentElement?.style.setProperty("--gallery-bg", `url("${imageUrl.replace(/"/g, "%22")}")`);

            thumbnails.forEach((item) => {
                const isSelected = item === thumbnail;
                item.classList.toggle("is-active", isSelected);
                item.setAttribute("aria-pressed", isSelected.toString());
            });

            if (counter && imageIndex) {
                counter.textContent = `${imageIndex} / ${thumbnails.length}`;
            }
        });
    });
})();

// ---------- Tabs (specifications / about, comments / questions) ----------
(() => {
    document.querySelectorAll("[data-tabs]").forEach((root) => {
        const tabs = Array.from(root.querySelectorAll("[data-tab]"));
        const panels = Array.from(root.querySelectorAll("[data-tab-panel]"));
        const tabConditional = Array.from(root.querySelectorAll("[data-visible-for]"));

        if (tabs.length === 0) {
            return;
        }

        const activate = (name, { focus = false } = {}) => {
            tabs.forEach((tab) => {
                const isActive = tab.dataset.tab === name;
                tab.classList.toggle("is-active", isActive);
                tab.setAttribute("aria-selected", isActive.toString());
                tab.tabIndex = isActive ? 0 : -1;
                if (isActive && focus) {
                    tab.focus();
                }
            });

            panels.forEach((panel) => {
                panel.hidden = panel.dataset.tabPanel !== name;
            });

            tabConditional.forEach((el) => {
                el.hidden = el.dataset.visibleFor !== name;
            });
        };

        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => activate(tab.dataset.tab));
            tab.addEventListener("keydown", (event) => {
                if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") {
                    return;
                }

                event.preventDefault();
                const step = event.key === "ArrowRight" ? 1 : -1;
                const next = tabs[(index + step + tabs.length) % tabs.length];
                activate(next.dataset.tab, { focus: true });
            });
        });

        root.addEventListener("tabs:activate", (event) => activate(event.detail));
    });

    // "Ask a question" jumps to the questions tab of the reviews block.
    document.querySelectorAll("[data-open-tab]").forEach((button) => {
        button.addEventListener("click", () => {
            const target = document.querySelector(`[data-tabs] [data-tab="${button.dataset.openTab}"]`);
            if (!target) {
                return;
            }

            target.click();
            target.closest("[data-tabs]")?.scrollIntoView({ behavior: "smooth", block: "start" });
        });
    });
})();

// ---------- Review sorting ----------
(() => {
    const select = document.querySelector("[data-review-sort]");
    const list = document.querySelector("[data-review-list]");

    if (!select || !list) {
        return;
    }

    const comparers = {
        newest: (a, b) => Date.parse(b.dataset.reviewDate) - Date.parse(a.dataset.reviewDate),
        oldest: (a, b) => Date.parse(a.dataset.reviewDate) - Date.parse(b.dataset.reviewDate),
        highest: (a, b) => Number(b.dataset.reviewRating) - Number(a.dataset.reviewRating),
        lowest: (a, b) => Number(a.dataset.reviewRating) - Number(b.dataset.reviewRating)
    };

    select.addEventListener("change", () => {
        const compare = comparers[select.value] ?? comparers.newest;
        Array.from(list.children)
            .sort(compare)
            .forEach((item) => list.appendChild(item));
    });
})();
