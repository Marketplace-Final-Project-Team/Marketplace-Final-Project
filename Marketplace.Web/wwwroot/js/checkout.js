(() => {
    const form = document.getElementById("checkout-form");
    if (!form) {
        return;
    }

    const steps = Array.from(form.querySelectorAll("[data-step]"));
    const indicators = Array.from(document.querySelectorAll("[data-step-indicator]"));
    const nextButton = form.querySelector("[data-step-next]");
    const backButton = form.querySelector("[data-step-back]");
    const submitButton = form.querySelector("[data-step-submit]");

    const addressRadios = () => Array.from(form.querySelectorAll('input[name="SelectedAddressId"]'));
    const newAddressFields = form.querySelector("[data-new-address-fields]");
    const newAddressInputs = newAddressFields
        ? Array.from(newAddressFields.querySelectorAll("input"))
        : [];

    let current = 1;

    const isNewAddressChosen = () => {
        const radios = addressRadios();
        if (radios.length === 0) {
            return true;
        }
        const chosen = radios.find((radio) => radio.checked);
        return !chosen || chosen.value === "";
    };

    const updateAddressOptionStyling = () => {
        form.querySelectorAll(".checkout-address-option").forEach((option) => {
            const radio = option.querySelector('input[name="SelectedAddressId"]');
            option.classList.toggle("is-selected", !!radio && radio.checked);
        });
    };

    const updateReviewAddress = () => {
        const summary = form.querySelector("[data-review-address]");
        if (!summary) {
            return;
        }

        if (!isNewAddressChosen()) {
            const chosen = addressRadios().find((radio) => radio.checked);
            const label = chosen ? form.querySelector(`[data-address-label="${chosen.value}"]`) : null;
            summary.textContent = label ? label.textContent.trim() : "";
            return;
        }

        const country = form.querySelector('[name="Country"]')?.value.trim() || "";
        const city = form.querySelector('[name="City"]')?.value.trim() || "";
        const street = form.querySelector('[name="Street"]')?.value.trim() || "";
        const parts = [country, city, street].filter(Boolean);
        summary.textContent = parts.length > 0 ? parts.join(", ") : "Адресу не вказано";
    };

    const syncNewAddressFieldsState = () => {
        const showNewAddressFields = isNewAddressChosen();

        if (newAddressFields) {
            newAddressFields.hidden = addressRadios().length > 0 && !showNewAddressFields;
        }

        newAddressInputs.forEach((input) => {
            input.required = showNewAddressFields;
        });

        updateAddressOptionStyling();
        updateReviewAddress();
    };

    addressRadios().forEach((radio) => radio.addEventListener("change", syncNewAddressFieldsState));
    newAddressInputs.forEach((input) => input.addEventListener("input", updateReviewAddress));

    const showStep = (stepNumber) => {
        steps.forEach((step) => {
            step.hidden = Number(step.dataset.step) !== stepNumber;
        });

        indicators.forEach((indicator) => {
            const n = Number(indicator.dataset.stepIndicator);
            indicator.classList.toggle("is-active", n === stepNumber);
            indicator.classList.toggle("is-complete", n < stepNumber);
        });

        if (backButton) backButton.hidden = stepNumber === 1;
        if (nextButton) nextButton.hidden = stepNumber === steps.length;
        if (submitButton) submitButton.hidden = stepNumber !== steps.length;

        current = stepNumber;

        if (stepNumber === steps.length) {
            updateReviewAddress();
        }

        window.scrollTo({ top: form.getBoundingClientRect().top + window.scrollY - 90, behavior: "smooth" });
    };

    const validateStep = (stepNumber) => {
        const step = steps.find((s) => Number(s.dataset.step) === stepNumber);
        if (!step) {
            return true;
        }

        const fields = Array.from(step.querySelectorAll("input, select, textarea")).filter(
            (el) => !el.disabled && !el.hidden
        );

        for (const field of fields) {
            if (!field.checkValidity()) {
                field.reportValidity();
                return false;
            }
        }

        return true;
    };

    if (nextButton) {
        nextButton.addEventListener("click", () => {
            if (!validateStep(current)) {
                return;
            }
            if (current < steps.length) {
                showStep(current + 1);
            }
        });
    }

    if (backButton) {
        backButton.addEventListener("click", () => {
            if (current > 1) {
                showStep(current - 1);
            }
        });
    }

    syncNewAddressFieldsState();
    showStep(1);

    const PROMO_CODES = { WELCOME10: 0.1 };

    const promoInput = form.querySelector("[data-promo-input]");
    const promoApply = form.querySelector("[data-promo-apply]");
    const promoMessage = form.querySelector("[data-promo-message]");
    const promoSummaryRow = document.querySelector("[data-promo-summary-row]");
    const promoSummaryValue = document.querySelector("[data-promo-summary-value]");
    const totalEl = document.querySelector("[data-order-total]");

    if (promoApply && promoInput && totalEl) {
        const baseTotal = parseFloat(totalEl.dataset.orderTotal.replace(",", "."));

        const formatCurrency = (value) =>
            value.toLocaleString("uk-UA", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

        const showMessage = (text, isError) => {
            if (!promoMessage) return;
            promoMessage.textContent = text;
            promoMessage.hidden = false;
            promoMessage.classList.toggle("checkout-promo__message--error", !!isError);
        };

        promoApply.addEventListener("click", () => {
            const code = promoInput.value.trim().toUpperCase();
            const rate = PROMO_CODES[code];

            if (!code) {
                showMessage("Введіть промокод.", true);
                return;
            }

            if (!rate) {
                showMessage("Такий промокод не знайдено або він вже не діє.", true);
                if (promoSummaryRow) promoSummaryRow.hidden = true;
                totalEl.textContent = `${formatCurrency(baseTotal)} ₴`;
                return;
            }

            const discount = baseTotal * rate;
            const newTotal = baseTotal - discount;

            showMessage(`Промокод ${code} застосовано: знижка ${Math.round(rate * 100)}%.`, false);
            if (promoSummaryRow && promoSummaryValue) {
                promoSummaryRow.hidden = false;
                promoSummaryValue.textContent = `−${formatCurrency(discount)} ₴`;
            }
            totalEl.textContent = `${formatCurrency(newTotal)} ₴`;
        });
    }
})();
