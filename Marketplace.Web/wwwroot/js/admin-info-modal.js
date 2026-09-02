(() => {
    const modals = Array.from(document.querySelectorAll("[data-info-modal]"));
    if (modals.length === 0) {
        return;
    }

    const openButtons = document.querySelectorAll("[data-info-open]");
    openButtons.forEach((button) => {
        button.addEventListener("click", () => {
            const modal = document.querySelector(`[data-info-modal="${button.dataset.infoOpen}"]`);
            if (modal) {
                modal.hidden = false;
            }
        });
    });

    modals.forEach((modal) => {
        const closeModal = () => {
            modal.hidden = true;
        };

        modal.querySelectorAll("[data-info-close]").forEach((closeBtn) => {
            closeBtn.addEventListener("click", closeModal);
        });

        modal.addEventListener("click", (event) => {
            if (event.target === modal) {
                closeModal();
            }
        });
    });
})();
