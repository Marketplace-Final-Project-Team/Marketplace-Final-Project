(() => {
    document.querySelectorAll(".support-faq-item__question").forEach((button) => {
        button.addEventListener("click", () => {
            const item = button.closest(".support-faq-item");
            const answer = item.querySelector(".support-faq-item__answer");
            const isOpen = item.classList.toggle("is-open");
            button.setAttribute("aria-expanded", isOpen ? "true" : "false");
            if (answer) {
                answer.hidden = !isOpen;
            }
        });
    });

    const chatStub = document.querySelector("[data-chat-stub]");
    const openButtons = document.querySelectorAll("[data-open-chat-stub]");
    const closeButton = document.querySelector("[data-close-chat-stub]");

    if (chatStub) {
        openButtons.forEach((button) => {
            button.addEventListener("click", () => {
                chatStub.hidden = false;
            });
        });

        closeButton?.addEventListener("click", () => {
            chatStub.hidden = true;
        });

        chatStub.addEventListener("click", (event) => {
            if (event.target === chatStub) {
                chatStub.hidden = true;
            }
        });
    }

    // ---------- Відправка заявки на загальне звернення ----------
    const submitButton = document.querySelector("[data-ticket-submit]");
    if (submitButton) {
        submitButton.addEventListener("click", async () => {
            const formWrap = document.querySelector("[data-ticket-form]");
            const pendingWrap = document.querySelector("[data-ticket-pending]");
            const categorySelect = document.querySelector("[data-ticket-category]");
            const messageField = document.querySelector("[data-ticket-message]");
            const errorField = document.querySelector("[data-ticket-error]");

            const category = categorySelect.value;
            const message = messageField.value.trim();

            const showError = (text) => {
                if (errorField) {
                    errorField.textContent = text;
                    errorField.hidden = false;
                }
            };

            if (errorField) {
                errorField.hidden = true;
            }

            if (!category) {
                showError(__t("Оберіть тему звернення."));
                return;
            }
            if (message.length < 10) {
                showError(__t("Опишіть ситуацію детальніше — щонайменше 10 символів."));
                return;
            }

            submitButton.disabled = true;
            submitButton.textContent = __t("Надсилання...");

            try {
                const formData = new FormData();
                formData.append("category", category);
                formData.append("message", message);

                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenInput) {
                    formData.append("__RequestVerificationToken", tokenInput.value);
                }

                const response = await fetch("/Support/CreateTicket", {
                    method: "POST",
                    body: formData
                });

                const result = await response.json();

                if (result.success) {
                    formWrap.hidden = true;
                    if (pendingWrap) {
                        pendingWrap.hidden = false;
                    }

                    // Перезавантажуємо сторінку — тепер сервер віддасть правильний блок
                    // "Очікуйте на відповідь" з data-ticket-poll, і реальне опитування запуститься.
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    showError(result.message || __t("Не вдалося надіслати заявку."));
                    submitButton.disabled = false;
                    submitButton.textContent = __t("Надіслати заявку");
                }
            } catch (error) {
                console.error("Помилка відправки заявки:", error);
                showError(__t("Сталася помилка з'єднання із сервером."));
                submitButton.disabled = false;
                submitButton.textContent = __t("Надіслати заявку");
            }
        });
    }

    // ---------- Опитування статусу заявки, поки вона Pending ----------
    const pollTarget = document.querySelector("[data-ticket-poll]");
    if (pollTarget) {
        const ticketId = pollTarget.dataset.ticketPoll;

        setInterval(async () => {
            try {
                const response = await fetch(`/Support/GetTicketStatus?ticketId=${ticketId}`);
                if (!response.ok) {
                    return;
                }

                const data = await response.json();

                if (data.status === "Open") {
                    window.location.reload();
                }
            } catch (error) {
                console.error("Помилка опитування статусу заявки:", error);
            }
        }, 5000);
    }
})();