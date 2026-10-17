(() => {
    const modal = document.querySelector("[data-admin-modal]");
    if (!modal) {
        return;
    }

    const body = modal.querySelector("[data-admin-modal-body]");

    const openModal = async (productId) => {
        body.innerHTML = `<p class="admin-empty">${__t("Завантаження...")}</p>`;
        modal.hidden = false;

        try {
            const response = await fetch(`/Admin/ProductDetailsPartial/${productId}`);
            if (!response.ok) {
                throw new Error("Request failed");
            }
            body.innerHTML = await response.text();
        } catch {
            body.innerHTML = `<p class="admin-empty">${__t("Не вдалося завантажити дані товару.")}</p>`;
        }
    };

    const closeModal = () => {
        modal.hidden = true;
        body.innerHTML = "";
    };

    document.querySelectorAll("[data-product-open]").forEach((trigger) => {
        trigger.addEventListener("click", () => openModal(trigger.dataset.productOpen));
    });

    modal.querySelectorAll("[data-admin-modal-close]").forEach((btn) => {
        btn.addEventListener("click", closeModal);
    });

    modal.addEventListener("click", (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !modal.hidden) {
            closeModal();
        }
    });
})();
