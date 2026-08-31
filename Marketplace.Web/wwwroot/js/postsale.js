(() => {
    const modals = Array.from(document.querySelectorAll("[data-postsale-modal]"));
    if (modals.length === 0) {
        return;
    }

    const openButtons = document.querySelectorAll("[data-postsale-open]");
    openButtons.forEach((button) => {
        button.addEventListener("click", () => {
            const modal = document.querySelector(`[data-postsale-modal="${button.dataset.postsaleOpen}"]`);
            if (modal) {
                const itemIdField = modal.querySelector("[data-postsale-item-id-field]");
                if (itemIdField && button.dataset.orderItemId) {
                    itemIdField.value = button.dataset.orderItemId;
                }
                modal.hidden = false;
            }
        });
    });

    modals.forEach((modal) => {
        const closeModal = () => {
            modal.hidden = true;
        };

        modal.querySelectorAll("[data-postsale-close]").forEach((closeBtn) => {
            closeBtn.addEventListener("click", closeModal);
        });

        modal.addEventListener("click", (event) => {
            if (event.target === modal) {
                closeModal();
            }
        });
    });

    const photosInput = document.querySelector('input[type="file"][name="photos"]');
    if (photosInput) {
        photosInput.addEventListener("change", () => {
            if (photosInput.files.length > 5) {
                photosInput.setCustomValidity("Можна додати не більше 5 фото.");
            } else {
                photosInput.setCustomValidity("");
            }
        });
    }
})();
