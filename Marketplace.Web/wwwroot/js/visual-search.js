(() => {
    const modal = document.querySelector("[data-visual-search-modal]");
    if (!modal) {
        return;
    }

    const intro = modal.querySelector("[data-visual-search-intro]");
    const loading = modal.querySelector("[data-visual-search-loading]");
    const result = modal.querySelector("[data-visual-search-result]");
    const fileInput = modal.querySelector("[data-visual-search-input]");
    const previewLoading = modal.querySelector("[data-visual-search-preview]");
    const previewResult = modal.querySelector("[data-visual-search-preview-2]");
    const fallbackGrid = modal.querySelector("[data-visual-search-fallback]");

    const escapeHtml = (value) => {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    };

    const resetModal = () => {
        intro.hidden = false;
        loading.hidden = true;
        result.hidden = true;
        fileInput.value = "";
    };

    const openModal = () => {
        resetModal();
        modal.hidden = false;
    };

    const closeModal = () => {
        modal.hidden = true;
    };

    document.querySelectorAll("[data-open-visual-search]").forEach((btn) => {
        btn.addEventListener("click", openModal);
    });

    modal.querySelectorAll("[data-close-visual-search]").forEach((btn) => {
        btn.addEventListener("click", closeModal);
    });

    modal.addEventListener("click", (event) => {
        if (event.target === modal) closeModal();
    });

    fileInput?.addEventListener("change", () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        const imageUrl = URL.createObjectURL(file);
        previewLoading.src = imageUrl;
        previewResult.src = imageUrl;

        intro.hidden = true;
        loading.hidden = false;
        result.hidden = true;

        Promise.all([
            fetch("/Catalog/PopularPicks").then((r) => (r.ok ? r.json() : [])),
            new Promise((resolve) => setTimeout(resolve, 900)),
        ]).then(([items]) => {
            fallbackGrid.innerHTML = (items || [])
                .map((item) => {
                    const id = item.id || item.Id;
                    const name = item.name || item.Name;
                    const imageUrl2 = item.imageUrl || item.ImageUrl;
                    const media = imageUrl2
                        ? `<img src="${escapeHtml(imageUrl2)}" alt="" style="width:2rem;height:2rem;border-radius:0.4rem;object-fit:cover;" />`
                        : "";
                    return `<a href="/Catalog/Details/${encodeURIComponent(id)}" class="visual-search-result__item">${media}<span>${escapeHtml(name)}</span></a>`;
                })
                .join("");

            loading.hidden = true;
            result.hidden = false;
        });
    });
})();
