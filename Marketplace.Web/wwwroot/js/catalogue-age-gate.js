(function () {
    const modal = document.getElementById('ageVerificationModal');

    if (!modal)
    {
        return;
    }

    const productInput = (modal.querySelector('#modalProductId'));
    const adultConfirmed = modal.dataset.adultConfirmed === 'true';

    const openModal = (productId) => {
        if (productInput)
        {
            productInput.value = productId || '';
        }

        modal.classList.remove('d-none');
    };

    const closeModal = () => modal.classList.add('d-none');

    document.addEventListener('click', (event) => {
        const target = event.target instanceof Element ? event.target : null;

        if (!target)
        {
            return;
        }

        if (target.closest('[data-age-open]'))
        {
            openModal(null);
            return;
        }

        if (target.closest('[data-age-close]'))
        {
            closeModal();
            return;
        }

        const navigate = (target.closest('[data-age-navigate]'));
        if (navigate) {
            const restricted = navigate.dataset.restricted === 'true';
            if (restricted && !adultConfirmed)
            {
                openModal(navigate.dataset.productId);
            }
            else if (navigate.dataset.detailsUrl)
            {
                window.location.href = navigate.dataset.detailsUrl;
            }
        }
    });
})();