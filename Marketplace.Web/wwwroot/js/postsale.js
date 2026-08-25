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
})();
