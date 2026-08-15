(() => {
    const modal = document.querySelector("[data-payout-modal]");
    if (!modal) {
        return;
    }

    document.querySelectorAll("[data-open-payout-request]").forEach((btn) => {
        btn.addEventListener("click", () => {
            modal.hidden = false;
        });
    });

    const closeModal = () => {
        modal.hidden = true;
    };

    modal.querySelectorAll("[data-close-payout]").forEach((btn) => {
        btn.addEventListener("click", closeModal);
    });

    modal.addEventListener("click", (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    modal.querySelector("[data-submit-payout]")?.addEventListener("click", () => {
        modal.querySelector("[data-payout-form]").hidden = true;
        modal.querySelector("[data-payout-success]").hidden = false;
    });
})();
