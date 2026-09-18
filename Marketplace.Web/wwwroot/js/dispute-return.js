(() => {
    console.log('[dispute-return.js] завантажено, шукаю кнопки...');

    const buttons = document.querySelectorAll('[data-dispute-submit]');
    console.log('[dispute-return.js] знайдено кнопок:', buttons.length);

    buttons.forEach((button) => {
        button.addEventListener('click', async () => {
            console.log('[dispute-return.js] клік по кнопці', button.dataset.requestId);

            const requestId = button.dataset.requestId;
            const businessId = button.dataset.businessId;
            const orderId = button.dataset.orderId;

            const formWrap = document.querySelector(`[data-dispute-form="${requestId}"]`);
            const successWrap = document.querySelector(`[data-dispute-success="${requestId}"]`);
            const reasonField = formWrap.querySelector('[data-dispute-reason]');
            const photosField = formWrap.querySelector('[data-dispute-photos]');
            const reason = reasonField.value.trim();

            if (reason.length < 10) {
                reasonField.setCustomValidity('Мінімум 10 символів.');
                reasonField.reportValidity();
                return;
            }
            reasonField.setCustomValidity('');

            button.disabled = true;
            button.textContent = 'Надсилання...';

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
                } else {
                    console.warn('[dispute-return.js] antiforgery токен не знайдено на сторінці!');
                }

                const response = await fetch('/Business/DisputeReturn', {
                    method: 'POST',
                    body: formData
                });

                console.log('[dispute-return.js] статус відповіді:', response.status);

                if (!response.ok) {
                    alert('Сервер повернув помилку (' + response.status + '). Перевірте консоль.');
                    button.disabled = false;
                    button.textContent = 'Надіслати на розгляд адміну';
                    return;
                }

                const result = await response.json();
                console.log('[dispute-return.js] результат:', result);

                if (result.success) {
                    formWrap.hidden = true;
                    successWrap.hidden = false;

                    setTimeout(() => {
                        window.location.href = `/Support/RefundChat?requestId=${result.requestId}&isSellerChannel=true`;
                    }, 2000);
                } else {
                    alert('Помилка: ' + (result.message || 'Не вдалося надіслати заявку.'));
                    button.disabled = false;
                    button.textContent = 'Надіслати на розгляд адміну';
                }
            } catch (error) {
                console.error('[dispute-return.js] помилка відправки:', error);
                alert("Сталася помилка з'єднання із сервером. Дивіться консоль (F12).");
                button.disabled = false;
                button.textContent = 'Надіслати на розгляд адміну';
            }
        });
    });
})();