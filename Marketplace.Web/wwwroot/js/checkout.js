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
            updateReviewCarrier();
            updateReviewPayment();
        }

        window.scrollTo({ top: form.getBoundingClientRect().top + window.scrollY - 90, behavior: "smooth" });
    };

    const carrierLabel = () => {
        const carrier = form.querySelector('input[name="DeliveryCarrier"]:checked')?.value;
        if (carrier === "NovaPoshta") {
            const cityName = form.querySelector("[data-np-city-name]")?.value;
            const branchName = form.querySelector("[data-np-branch-name]")?.value;
            return cityName && branchName ? `Нова пошта — ${branchName}, ${cityName}` : "Нова пошта";
        }
        if (carrier === "Ukrposhta" || carrier === "Meest") {
            const label = carrier === "Ukrposhta" ? "Укрпошта" : "Meest Express";
            const city = form.querySelector('[name="DeliveryCity"]')?.value;
            const street = form.querySelector('[name="DeliveryStreet"]')?.value;
            return city && street ? `${label} — ${city}, ${street}` : label;
        }
        return "—";
    };

    const updateReviewCarrier = () => {
        const summary = form.querySelector("[data-review-carrier]");
        if (summary) summary.textContent = carrierLabel();
    };

    const paymentLabel = () => {
        const method = form.querySelector('input[name="PaymentMethod"]:checked')?.value;
        return method === "CashOnDelivery" ? "Оплата при отриманні" : "LiqPay (онлайн)";
    };

    const updateReviewPayment = () => {
        const summary = form.querySelector("[data-review-payment]");
        if (summary) summary.textContent = paymentLabel();
    };

    const validateStep = (stepNumber) => {
        const step = steps.find((s) => Number(s.dataset.step) === stepNumber);
        if (!step) {
            return true;
        }

        const fields = Array.from(step.querySelectorAll("input, select, textarea")).filter(
            // el.hidden only reflects the element's own [hidden] attribute, not an
            // ancestor's — a required field inside a hidden carrier panel (e.g. the
            // Ukrposhta/Meest address fields while NovaPoshta is selected) would
            // otherwise still be treated as visible and block validation.
            (el) => !el.disabled && !el.hidden && el.type !== "hidden" && el.offsetParent !== null
        );

        for (const field of fields) {
            if (!field.checkValidity()) {
                field.reportValidity();
                return false;
            }
        }

        if (stepNumber === 2) {
            const carrier = form.querySelector('input[name="DeliveryCarrier"]:checked')?.value;
            const branchError = form.querySelector("[data-np-branch-error]");
            if (carrier === "NovaPoshta" && !form.querySelector("[data-np-branch-ref]")?.value) {
                if (branchError) branchError.hidden = false;
                return false;
            }
            if (branchError) branchError.hidden = true;
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

    // ---------- Delivery carrier selection ----------

    const carrierRadios = Array.from(form.querySelectorAll("[data-carrier-radio]"));
    const carrierPanels = Array.from(form.querySelectorAll("[data-carrier-panel]"));

    const syncCarrierPanels = () => {
        const carrier = form.querySelector('input[name="DeliveryCarrier"]:checked')?.value;
        carrierPanels.forEach((panel) => {
            const panelCarriers = panel.dataset.carrierPanel.split(",");
            panel.hidden = !panelCarriers.includes(carrier);
        });
    };

    carrierRadios.forEach((radio) => radio.addEventListener("change", syncCarrierPanels));
    syncCarrierPanels();

    const cityInput = form.querySelector("[data-np-city-input]");
    const cityRefInput = form.querySelector("[data-np-city-ref]");
    const cityNameInput = form.querySelector("[data-np-city-name]");
    const branchSelect = form.querySelector("[data-np-branch-select]");
    const branchRefInput = form.querySelector("[data-np-branch-ref]");
    const branchNameInput = form.querySelector("[data-np-branch-name]");

    if (cityInput && branchSelect) {
        const citySuggest = document.createElement("div");
        citySuggest.className = "checkout-np-suggest";
        citySuggest.hidden = true;
        cityInput.insertAdjacentElement("afterend", citySuggest);

        let cityDebounce;
        const escapeHtml = (value) => {
            const div = document.createElement("div");
            div.textContent = value ?? "";
            return div.innerHTML;
        };

        const closeCitySuggest = () => {
            citySuggest.hidden = true;
            citySuggest.innerHTML = "";
        };

        const resetBranches = (placeholder) => {
            branchSelect.innerHTML = `<option value="">${escapeHtml(placeholder)}</option>`;
            branchSelect.disabled = true;
            branchRefInput.value = "";
            branchNameInput.value = "";
        };

        const loadBranches = (cityRef, preselectBranchRef) => {
            resetBranches("Завантаження відділень…");
            fetch(`/Delivery/Branches?cityId=${encodeURIComponent(cityRef)}`)
                .then((response) => (response.ok ? response.json() : []))
                .then((branches) => {
                    if (!Array.isArray(branches) || branches.length === 0) {
                        resetBranches("Відділень не знайдено");
                        return;
                    }

                    branchSelect.innerHTML =
                        `<option value="">Оберіть відділення</option>` +
                        branches
                            .map((branch) => {
                                const id = branch.id || branch.Id;
                                const name = branch.name || branch.Name;
                                const address = branch.address || branch.Address || "";
                                return `<option value="${escapeHtml(id)}" data-name="${escapeHtml(name)}">${escapeHtml(name)}${address ? " — " + escapeHtml(address) : ""}</option>`;
                            })
                            .join("");
                    branchSelect.disabled = false;

                    if (preselectBranchRef) {
                        branchSelect.value = preselectBranchRef;
                        if (branchSelect.value === preselectBranchRef) {
                            branchRefInput.value = preselectBranchRef;
                            branchNameInput.value = branchSelect.selectedOptions[0].dataset.name;
                        }
                    }
                })
                .catch(() => resetBranches("Не вдалося завантажити відділення"));
        };

        cityInput.addEventListener("input", () => {
            cityRefInput.value = "";
            cityNameInput.value = "";
            resetBranches("Спочатку оберіть місто");

            clearTimeout(cityDebounce);
            const term = cityInput.value.trim();
            if (term.length < 2) {
                closeCitySuggest();
                return;
            }

            cityDebounce = setTimeout(() => {
                fetch(`/Delivery/Cities?search=${encodeURIComponent(term)}&limit=8`)
                    .then((response) => (response.ok ? response.json() : []))
                    .then((cities) => {
                        if (!Array.isArray(cities) || cities.length === 0) {
                            citySuggest.innerHTML = `<div class="checkout-np-suggest__empty">Місто не знайдено</div>`;
                            citySuggest.hidden = false;
                            return;
                        }

                        citySuggest.innerHTML = cities
                            .map((city) => {
                                const id = city.id || city.Id;
                                const name = city.name || city.Name;
                                return `<button type="button" data-city-ref="${escapeHtml(id)}" data-city-name="${escapeHtml(name)}">${escapeHtml(name)}</button>`;
                            })
                            .join("");
                        citySuggest.hidden = false;

                        citySuggest.querySelectorAll("button").forEach((btn) => {
                            btn.addEventListener("click", () => {
                                cityInput.value = btn.dataset.cityName;
                                cityRefInput.value = btn.dataset.cityRef;
                                cityNameInput.value = btn.dataset.cityName;
                                closeCitySuggest();
                                loadBranches(btn.dataset.cityRef);
                            });
                        });
                    })
                    .catch(() => {
                        citySuggest.innerHTML = `<div class="checkout-np-suggest__empty">Не вдалося завантажити міста</div>`;
                        citySuggest.hidden = false;
                    });
            }, 300);
        });

        branchSelect.addEventListener("change", () => {
            const option = branchSelect.selectedOptions[0];
            branchRefInput.value = branchSelect.value;
            branchNameInput.value = branchSelect.value ? option.dataset.name : "";
        });

        document.addEventListener("click", (event) => {
            if (!citySuggest.contains(event.target) && event.target !== cityInput) {
                closeCitySuggest();
            }
        });

        if (cityRefInput.value) {
            loadBranches(cityRefInput.value, branchRefInput.value);
        }
    }

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

        promoApply.addEventListener("click", async () => {
            const code = promoInput.value.trim().toUpperCase();
            promoInput.value = code;

            if (!code) {
                showMessage("Введіть промокод.", true);
                return;
            }

            promoApply.disabled = true;
            try {
                const response = await fetch(`/Order/ValidatePromoCode?code=${encodeURIComponent(code)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const result = await response.json();

                if (!result.succeeded) {
                    showMessage(result.message || "Такий промокод не знайдено або він вже не діє.", true);
                    if (promoSummaryRow) promoSummaryRow.hidden = true;
                    totalEl.textContent = `${formatCurrency(baseTotal)} ₴`;
                    return;
                }

                const newTotal = baseTotal - result.discountAmount;

                showMessage(`Промокод ${result.code} застосовано: знижка ${result.percent}% на товари магазину «${result.storeName}».`, false);
                if (promoSummaryRow && promoSummaryValue) {
                    promoSummaryRow.hidden = false;
                    promoSummaryValue.textContent = `−${formatCurrency(result.discountAmount)} ₴`;
                }
                totalEl.textContent = `${formatCurrency(newTotal)} ₴`;
            } catch {
                showMessage("Не вдалося перевірити промокод. Спробуйте ще раз.", true);
            } finally {
                promoApply.disabled = false;
            }
        });
    }
})();
