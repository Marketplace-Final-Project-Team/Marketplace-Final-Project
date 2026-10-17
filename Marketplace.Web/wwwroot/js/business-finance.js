(() => {
    const modal = document.querySelector("[data-payout-modal]");
    if (!modal) {
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    let savedCards = window.__savedCards || [];

    document.querySelectorAll("[data-open-payout-request]").forEach((btn) => {
        btn.addEventListener("click", () => {
            modal.hidden = false;
        });
    });

    const closeModal = () => { modal.hidden = true; };

    modal.querySelectorAll("[data-close-payout]").forEach((btn) => {
        btn.addEventListener("click", closeModal);
    });

    modal.addEventListener("click", (event) => {
        if (event.target === modal) closeModal();
    });

    const selector = modal.querySelector("[data-card-selector]");
    const selectorToggle = modal.querySelector("[data-card-selector-toggle]");
    const selectorMenu = modal.querySelector("[data-card-selector-menu]");
    const selectorCurrent = modal.querySelector("[data-card-selector-current]");
    const optionsContainer = modal.querySelector("[data-card-options]");
    const selectedCardIdInput = modal.querySelector("[data-selected-card-id]");
    const newCardFields = modal.querySelector("[data-new-card-fields]");
    const addCardBtn = modal.querySelector("[data-card-selector-add]");

    function renderCardOptions() {
        if (!optionsContainer) return;
        optionsContainer.innerHTML = savedCards.map(c => `
            <button type="button" class="card-selector__option" data-card-id="${c.id}">
                <span>💳 ${c.masked}</span>
                <span class="text-muted small">${c.recipient}</span>
            </button>
        `).join("");

        optionsContainer.querySelectorAll("[data-card-id]").forEach(opt => {
            opt.addEventListener("click", () => {
                const id = opt.dataset.cardId;
                const card = savedCards.find(c => String(c.id) === id);
                selectedCardIdInput.value = id;
                selectorCurrent.textContent = `💳 ${card.masked}`;
                newCardFields.hidden = true;
                selectorMenu.hidden = true;
            });
        });

        if (!selectedCardIdInput.value && savedCards.length > 0) {
            const defaultCard = savedCards.find(c => c.isDefault) || savedCards[0];
            selectedCardIdInput.value = defaultCard.id;
            selectorCurrent.textContent = `💳 ${defaultCard.masked}`;
        } else if (savedCards.length === 0) {
 
            selectedCardIdInput.value = "";
            selectorCurrent.textContent = __t("Нова картка");
            newCardFields.hidden = false;
        }
    }

    selectorToggle?.addEventListener("click", (e) => {
        e.stopPropagation();
        selectorMenu.hidden = !selectorMenu.hidden;
    });

    document.addEventListener("click", () => {
        if (selectorMenu) selectorMenu.hidden = true;
    });

    addCardBtn?.addEventListener("click", () => {
        selectedCardIdInput.value = "";
        selectorCurrent.textContent = __t("Нова картка");
        newCardFields.hidden = false;
        selectorMenu.hidden = true;
    });

    renderCardOptions();

    // ---------- Відправка заявки ----------
    modal.querySelector("[data-submit-payout]")?.addEventListener("click", async () => {
        const errorEl = modal.querySelector("[data-payout-error]");
        errorEl.hidden = true;

        const amount = modal.querySelector("[data-payout-amount]").value;
        const businessId = window.__withdrawalBusinessId;
        const savedCardId = selectedCardIdInput.value;

        if (!amount || Number(amount) <= 0) {
            errorEl.textContent = __t("Вкажіть суму більшу за нуль.");
            errorEl.hidden = false;
            return;
        }

        let cardNumber = "";
        let recipientName = "";
        let rememberCard = false;

        if (!savedCardId) {
            cardNumber = modal.querySelector("[data-payout-card]")?.value.trim() ?? "";
            recipientName = modal.querySelector("[data-payout-recipient]")?.value.trim() ?? "";
            rememberCard = modal.querySelector("[data-remember-card]")?.checked ?? false;

            if (!cardNumber || !recipientName) {
                errorEl.textContent = __t("Заповніть номер картки та отримувача, або оберіть збережену картку.");
                errorEl.hidden = false;
                return;
            }
        }

        const submitBtn = modal.querySelector("[data-submit-payout]");
        submitBtn.disabled = true;

        try {
            const body = new URLSearchParams({
                businessId,
                amount,
                savedCardId: savedCardId || "",
                cardNumber,
                recipientName,
                rememberCard: rememberCard ? "true" : "false",
                __RequestVerificationToken: token || ""
            });

            const response = await fetch("/Withdrawal/Create", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body
            });

            if (!response.ok) {
                const text = await response.text();
                errorEl.textContent = text || __t("Не вдалося надіслати запит.");
                errorEl.hidden = false;
                submitBtn.disabled = false;
                return;
            }

            modal.querySelector("[data-payout-form]").hidden = true;
            modal.querySelector("[data-payout-success]").hidden = false;
        } catch {
            errorEl.textContent = __t("Помилка мережі. Спробуйте ще раз.");
            errorEl.hidden = false;
            submitBtn.disabled = false;
        }
    });

    modal.querySelector("[data-payout-success] [data-close-payout]")?.addEventListener("click", () => {
        window.location.reload();
    });
})();

// ---------- Форма запланованої автовиплати ----------
(() => {
    const form = document.querySelector("[data-scheduled-payout-form]");
    if (!form) {
        return;
    }

    const errorEl = form.querySelector("[data-scheduled-error]");
    const successEl = form.querySelector("[data-scheduled-success]");
    const enabledCheck = form.querySelector("[data-scheduled-enabled]");
    const fieldsBlock = form.querySelector("[data-scheduled-fields]");
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    enabledCheck?.addEventListener("change", () => {
        if (fieldsBlock) fieldsBlock.hidden = !enabledCheck.checked;
    });

    form.querySelector("[data-save-scheduled-payout]")?.addEventListener("click", async () => {
        if (errorEl) errorEl.hidden = true;
        if (successEl) successEl.hidden = true;

        const businessId = form.dataset.businessId;
        const enabled = enabledCheck?.checked ?? false;
        const dateValue = form.querySelector("[data-scheduled-date]")?.value ?? "";
        const amount = form.querySelector("[data-scheduled-amount]")?.value ?? "";
        const savedCardId = form.querySelector("[data-scheduled-card]")?.value ?? "";

     
        const dayOfMonth = dateValue ? new Date(dateValue + "T00:00:00").getDate() : "";

        if (enabled && (!amount || Number(amount) <= 0)) {
            if (errorEl) {
                errorEl.textContent = __t("Вкажіть суму більшу за нуль.");
                errorEl.hidden = false;
            }
            return;
        }

        const saveBtn = form.querySelector("[data-save-scheduled-payout]");
        saveBtn.disabled = true;

        try {
            const body = new URLSearchParams({
                businessId,
                enabled: enabled ? "true" : "false",
                dayOfMonth: enabled ? String(dayOfMonth) : "",
                amount: enabled ? amount : "",
                savedCardId: enabled ? savedCardId : "",
                __RequestVerificationToken: token || ""
            });

            const response = await fetch("/Withdrawal/SetScheduledPayoutSettings", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body
            });

            if (!response.ok) {
                const text = await response.text();
                if (errorEl) {
                    errorEl.textContent = text || __t("Не вдалося зберегти налаштування.");
                    errorEl.hidden = false;
                }
            } else if (successEl) {
                successEl.textContent = __t("Налаштування збережено.");
                successEl.hidden = false;
            }
        } catch {
            if (errorEl) {
                errorEl.textContent = __t("Помилка мережі. Спробуйте ще раз.");
                errorEl.hidden = false;
            }
        } finally {
            saveBtn.disabled = false;
        }
    });
})();