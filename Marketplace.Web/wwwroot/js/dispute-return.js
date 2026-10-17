(() => {
    const buttons = document.querySelectorAll('[data-dispute-submit]');

    buttons.forEach((button) => {
        button.addEventListener('click', async () => {
            const requestId = button.dataset.requestId;
            const businessId = button.dataset.businessId;
            const orderId = button.dataset.orderId;

            const formWrap = document.querySelector(`[data-dispute-form="${requestId}"]`);
            const successWrap = document.querySelector(`[data-dispute-success="${requestId}"]`);
            const reasonField = formWrap.querySelector('[data-dispute-reason]');
            const photosField = formWrap.querySelector('[data-dispute-photos]');
            const errorField = formWrap.querySelector('[data-dispute-error]');
            const reason = reasonField.value.trim();

            const showError = (message) => {
                if (errorField) {
                    errorField.textContent = message;
                    errorField.hidden = false;
                }
            };

            if (errorField) {
                errorField.hidden = true;
            }

            if (reason.length < 10) {
                reasonField.setCustomValidity(__t('Мінімум 10 символів.'));
                reasonField.reportValidity();
                return;
            }
            reasonField.setCustomValidity('');

            button.disabled = true;
            button.textContent = __t('Надсилання...');

            try {
                const formData = new FormData();
                formData.append('businessId', businessId);
                formData.append('orderId', orderId);
                formData.append('requestId', requestId);
                formData.append('reason', reason);

                if (photosField && photosField.files.length > 0) {
                    for (const file of photosField.files) {
                        formData.append('photos', file);
                    }
                }

                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenInput) {
                    formData.append('__RequestVerificationToken', tokenInput.value);
                }

                const response = await fetch('/Business/DisputeReturn', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    showError(__t('Не вдалося надіслати заявку. Спробуйте ще раз.'));
                    button.disabled = false;
                    button.textContent = __t('Надіслати на розгляд адміну');
                    return;
                }

                const result = await response.json();

                if (result.success) {
                    formWrap.hidden = true;
                    successWrap.hidden = false;

                    setTimeout(() => {
                        window.location.href = `/Support/RefundChat?requestId=${result.requestId}&isSellerChannel=true`;
                    }, 2000);
                } else {
                    showError(result.message || __t('Не вдалося надіслати заявку.'));
                    button.disabled = false;
                    button.textContent = __t('Надіслати на розгляд адміну');
                }
            } catch (error) {
                console.error('Помилка відправки заявки:', error);
                showError(__t('Сталася помилка з’єднання із сервером.'));
                button.disabled = false;
                button.textContent = __t('Надіслати на розгляд адміну');
            }
        });
    });
})();
