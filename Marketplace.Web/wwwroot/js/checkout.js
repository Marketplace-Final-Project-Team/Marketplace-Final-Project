(() => {
    const form = document.getElementById("checkout-form");
    if (!form) {
        return;
    }

    const messages = JSON.parse(document.getElementById("checkout-messages").textContent);
    const t = (key) => messages[key] || key;

    const steps = Array.from(form.querySelectorAll("[data-step]"));
    const indicators = Array.from(document.querySelectorAll("[data-step-indicator]"));
    const nextButton = form.querySelector("[data-step-next]");
    const backButton = form.querySelector("[data-step-back]");
    const submitButton = form.querySelector("[data-step-submit]");

    const phoneNumber = form.querySelector("[data-phone-number]");
    const phoneFull = form.querySelector("[data-phone-full]");
    if (phoneNumber && phoneFull) {
        const nationalDigits = (value) => {
            let digits = value.replace(/[^0-9]/g, "");
            if (digits.length === 12 && digits.startsWith("380")) digits = digits.slice(3);
            else if (digits.length === 10 && digits.startsWith("0")) digits = digits.slice(1);
            return digits;
        };
        const syncPhone = () => {
            phoneNumber.setCustomValidity("");
            const position = phoneNumber.selectionStart;
            const clean = nationalDigits(phoneNumber.value).slice(0, 9);
            if (phoneNumber.value !== clean) {
                phoneNumber.value = clean;
                if (position !== null) phoneNumber.setSelectionRange(Math.min(position, clean.length), Math.min(position, clean.length));
            }
            phoneFull.value = `+380${clean}`;
        };
        phoneNumber.addEventListener("input", syncPhone);
        phoneNumber.addEventListener("change", syncPhone);
        phoneNumber.addEventListener("paste", (event) => {
            event.preventDefault();
            const pasted = nationalDigits(event.clipboardData.getData("text"));
            if (pasted.length > 9) {
                phoneNumber.setCustomValidity(phoneNumber.dataset.phoneInvalid);
                phoneNumber.reportValidity();
                return;
            }
            const start = phoneNumber.selectionStart ?? 0;
            const end = phoneNumber.selectionEnd ?? phoneNumber.value.length;
            phoneNumber.value = (phoneNumber.value.slice(0, start) + pasted + phoneNumber.value.slice(end)).slice(0, 9);
            phoneNumber.setSelectionRange(Math.min(start + pasted.length, 9), Math.min(start + pasted.length, 9));
            phoneNumber.dispatchEvent(new Event("input", { bubbles: true }));
        });
        syncPhone();
    }

    let current = 1;

    const updateReviewRecipient = () => {
        const summary = form.querySelector("[data-review-recipient]");
        if (summary) {
            summary.textContent = ["RecipientLastName", "RecipientFirstName"]
                .map((name) => form.querySelector(`[name="${name}"]`)?.value.trim())
                .filter(Boolean).join(" ");
        }
        const phone = form.querySelector("[data-review-phone]");
        if (phone) phone.textContent = phoneFull?.value.trim() || "-";
    };

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
            updateReviewRecipient();
            updateReviewCarrier();
            updateReviewPayment();
        }

        window.scrollTo({ top: form.getBoundingClientRect().top + window.scrollY - 90, behavior: "smooth" });
    };

    const carrierLabel = () => {
        const carrier = form.querySelector('input[name="DeliveryCarrier"]:checked')?.value;
        if (carrier === "NovaPoshta") {
            return t("Нова пошта");
        }
        if (carrier === "Ukrposhta" || carrier === "Meest") {
            return carrier === "Ukrposhta" ? t("Укрпошта") : "Meest Express";
        }
        return "-";
    };

    const updateReviewCarrier = () => {
        const summary = form.querySelector("[data-review-carrier]");
        if (summary) summary.textContent = carrierLabel();
        const address = form.querySelector("[data-review-address]");
        const novaPoshta = form.querySelector('input[name="DeliveryCarrier"]:checked')?.value === "NovaPoshta";
        const fields = novaPoshta
            ? ["[data-np-city-name]", "[data-np-branch-name]"]
            : ['[name="DeliveryCity"]', '[name="DeliveryStreet"]'];
        if (address) address.textContent = fields
            .map((selector) => form.querySelector(selector)?.value.trim())
            .filter(Boolean).join(", ") || "-";
    };

    const paymentLabel = () => {
        const method = form.querySelector('input[name="PaymentMethod"]:checked')?.value;
        return method === "CashOnDelivery" ? t("Оплата при отриманні") : t("LiqPay (онлайн)");
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
            if (field.required && ["text", "tel"].includes(field.type)) {
                field.value = field.value.trim();
            }
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

    // Validate hidden steps ourselves so the browser can focus the invalid field.
    form.noValidate = true;
    let submitting = false;
    form.addEventListener("submit", (event) => {
        if (submitting || event.defaultPrevented) {
            event.preventDefault();
            return;
        }
        // Enter in a field advances only one step; creation requires the review step.
        if (current < steps.length) {
            event.preventDefault();
            if (validateStep(current)) showStep(current + 1);
            return;
        }
        for (let step = 1; step <= steps.length; step++) {
            showStep(step);
            if (!validateStep(step)) {
                event.preventDefault();
                return;
            }
        }
        submitting = true;
        if (submitButton) submitButton.disabled = true;
        if (backButton) backButton.disabled = true;
    });
    window.addEventListener("pageshow", () => {
        submitting = false;
        if (submitButton) submitButton.disabled = false;
        if (backButton) backButton.disabled = false;
    });
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
        let citySearchVersion = 0;
        let branchLoadVersion = 0;
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
            branchLoadVersion++;
            branchSelect.innerHTML = `<option value="">${escapeHtml(placeholder)}</option>`;
            branchSelect.disabled = true;
            branchRefInput.value = "";
            branchNameInput.value = "";
        };

        const loadBranches = (cityRef, preselectBranchRef) => {
            resetBranches(t("Завантаження відділень…"));
            const loadVersion = branchLoadVersion;
            fetch(`/Delivery/Branches?cityId=${encodeURIComponent(cityRef)}`)
                .then((response) => (response.ok ? response.json() : []))
                .then((branches) => {
                    if (loadVersion !== branchLoadVersion || cityRefInput.value !== cityRef) return;
                    if (!Array.isArray(branches) || branches.length === 0) {
                        resetBranches(t("Відділень не знайдено"));
                        return;
                    }

                    branchSelect.innerHTML =
                        `<option value="">${escapeHtml(t("Оберіть відділення"))}</option>` +
                        branches
                            .map((branch) => {
                                const id = branch.id || branch.Id;
                                const name = branch.name || branch.Name;
                                const address = branch.address || branch.Address || "";
                                return `<option value="${escapeHtml(id)}" data-name="${escapeHtml(name)}">${escapeHtml(name)}${address ? " - " + escapeHtml(address) : ""}</option>`;
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
                .catch(() => {
                    if (loadVersion === branchLoadVersion && cityRefInput.value === cityRef) {
                        resetBranches(t("Не вдалося завантажити відділення"));
                    }
                });
        };

        cityInput.addEventListener("input", () => {
            const searchVersion = ++citySearchVersion;
            closeCitySuggest();
            cityRefInput.value = "";
            cityNameInput.value = "";
            resetBranches(t("Спочатку оберіть місто"));

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
                        if (searchVersion !== citySearchVersion) return;
                        if (!Array.isArray(cities) || cities.length === 0) {
                            citySuggest.innerHTML = `<div class="checkout-np-suggest__empty">${escapeHtml(t("Місто не знайдено"))}</div>`;
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
                                citySearchVersion++;
                                cityInput.value = btn.dataset.cityName;
                                cityRefInput.value = btn.dataset.cityRef;
                                cityNameInput.value = btn.dataset.cityName;
                                closeCitySuggest();
                                loadBranches(btn.dataset.cityRef);
                            });
                        });
                    })
                    .catch(() => {
                        if (searchVersion !== citySearchVersion) return;
                        citySuggest.innerHTML = `<div class="checkout-np-suggest__empty">${escapeHtml(t("Не вдалося завантажити міста"))}</div>`;
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
        promoInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                promoApply.click();
            }
        });
        const baseTotal = parseFloat(totalEl.dataset.orderTotal.replace(",", "."));

        const formatCurrency = (value) =>
            value.toLocaleString(document.documentElement.lang || "uk-UA", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

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
                showMessage(t("Введіть промокод."), true);
                return;
            }

            promoApply.disabled = true;
            try {
                const response = await fetch(`/Order/ValidatePromoCode?code=${encodeURIComponent(code)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const result = await response.json();

                if (!result.succeeded) {
                    showMessage(messages[result.message] || t("Такий промокод не знайдено або він вже не діє."), true);
                    if (promoSummaryRow) promoSummaryRow.hidden = true;
                    totalEl.textContent = `${formatCurrency(baseTotal)} ₴`;
                    return;
                }

                const newTotal = baseTotal - result.discountAmount;

                showMessage(t("Промокод {0} застосовано: знижка {1}% на товари магазину «{2}».").replace("{0}", result.code).replace("{1}", result.percent).replace("{2}", result.storeName), false);
                if (promoSummaryRow && promoSummaryValue) {
                    promoSummaryRow.hidden = false;
                    promoSummaryValue.textContent = `−${formatCurrency(result.discountAmount)} ₴`;
                }
                totalEl.textContent = `${formatCurrency(newTotal)} ₴`;
            } catch {
                showMessage(t("Не вдалося перевірити промокод. Спробуйте ще раз."), true);
            } finally {
                promoApply.disabled = false;
            }
        });
    }
})();
