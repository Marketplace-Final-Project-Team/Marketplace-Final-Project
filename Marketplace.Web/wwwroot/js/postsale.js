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

    const returnForm = document.querySelector('form[asp-action="RequestReturn"]') || document.querySelector('.postsale-modal form');

    if (returnForm) {
  
        if (returnForm.dataset.ajaxInitialized) return;
        returnForm.dataset.ajaxInitialized = "true";


        const modalCard = returnForm.closest('.postsale-modal__card');
        let successAnimation = modalCard.querySelector('#successAnimation');

        if (!successAnimation && modalCard) {
            successAnimation = document.createElement('div');
            successAnimation.id = 'successAnimation';
            successAnimation.style.cssText = 'display: none; text-align: center; padding: 2rem;';
            successAnimation.innerHTML = `
                <div class="spinner-border text-primary" role="status"></div>
                <h4 class="mt-3">Заявку створено!</h4>
                <p class="text-muted">Переводимо вас у чат з адміністратором...</p>
            `;
            modalCard.appendChild(successAnimation);
        }

        returnForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const submitBtn = returnForm.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = "Надсилання...";
            }

            const formData = new FormData(returnForm);

            try {
                const response = await fetch(returnForm.action, {
                    method: 'POST',
                    body: formData
                });

                const result = await response.json();

                if (result.success) {
          
                    returnForm.style.display = "none";
                    if (successAnimation) {
                        successAnimation.style.display = "block";
                    }

           
                    setTimeout(() => {
                        window.location.href = `/Support/RefundChat?requestId=${result.requestId}`;
                    }, 2000);
                } else {
                    alert("Помилка: " + (result.message || "Не вдалося створити заявку."));
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.textContent = "Надіслати заявку";
                    }
                }
            } catch (error) {
                console.error("Помилка відправки:", error);
                alert("Сталася помилка з'єднання із сервером.");
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.textContent = "Надіслати заявку";
                }
            }
        });
    }
})();