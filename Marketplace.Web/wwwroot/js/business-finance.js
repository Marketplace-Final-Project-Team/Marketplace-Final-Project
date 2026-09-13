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

    modal.querySelector("[data-submit-payout]")?.addEventListener("click", async () => {
        const errorEl = modal.querySelector("[data-payout-error]");
        errorEl.hidden = true;

        const amount = modal.querySelector("[data-payout-amount]").value;
        const cardNumber = modal.querySelector("[data-payout-card]").value.trim();
        const recipientName = modal.querySelector("[data-payout-recipient]").value.trim();
        const businessId = window.__withdrawalBusinessId;
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        if (!amount || Number(amount) <= 0) {
            errorEl.textContent = "Вкажіть суму більшу за нуль.";
            errorEl.hidden = false;
            return;
        }
        if (!cardNumber || !recipientName) {
            errorEl.textContent = "Заповніть номер картки та отримувача.";
            errorEl.hidden = false;
            return;
        }

        const submitBtn = modal.querySelector("[data-submit-payout]");
        submitBtn.disabled = true;

        try {
            const body = new URLSearchParams({
                businessId,
                amount,
                cardNumber,
                recipientName,
                __RequestVerificationToken: token || ""
            });

            const response = await fetch("/Withdrawal/Create", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body
            });

            if (!response.ok) {
                const text = await response.text();
                errorEl.textContent = text || "Не вдалося надіслати запит.";
                errorEl.hidden = false;
                submitBtn.disabled = false;
                return;
            }

            modal.querySelector("[data-payout-form]").hidden = true;
            modal.querySelector("[data-payout-success]").hidden = false;
        } catch {
            errorEl.textContent = "Помилка мережі. Спробуйте ще раз.";
            errorEl.hidden = false;
            submitBtn.disabled = false;
        }
    });

    modal.querySelector("[data-payout-success] [data-close-payout]")?.addEventListener("click", () => {
        window.location.reload();
    });
})();
