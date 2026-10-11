(() => {
    const buttons = Array.from(document.querySelectorAll("[data-view-toggle]"));
    const views = Array.from(document.querySelectorAll("[data-products-view]"));
    if (buttons.length === 0 || views.length === 0) {
        return;
    }

    const STORAGE_KEY = "home-products-view";

    const setView = (view) => {
        views.forEach((el) => {
            el.hidden = el.dataset.productsView !== view;
        });
        buttons.forEach((btn) => {
            btn.classList.toggle("is-active", btn.dataset.viewToggle === view);
        });
        try {
            localStorage.setItem(STORAGE_KEY, view);
        } catch {
            // localStorage unavailable - view choice just won't persist across reloads.
        }
    };

    buttons.forEach((btn) => {
        btn.addEventListener("click", () => setView(btn.dataset.viewToggle));
    });

    let saved = "grid";
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored && views.some((el) => el.dataset.productsView === stored)) {
            saved = stored;
        }
    } catch {
        // localStorage unavailable - default to grid.
    }

    setView(saved);
})();
