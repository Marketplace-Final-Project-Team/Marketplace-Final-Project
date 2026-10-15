(function () {
    const select = document.querySelector('[data-saved-address-select]');
    const fields = document.querySelector('[data-new-address-fields]');
    const city = document.querySelector('input[name="DeliveryCity"]');
    const street = document.querySelector('input[name="DeliveryStreet"]');

    if (!select || !fields || !city || !street) {
        return;
    }

    const setValue = (input, value) => {
        input.value = value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
    };

    select.addEventListener('change', () => {
        if (select.value === 'new') {
            setValue(city, '');
            setValue(street, '');
            fields.hidden = false;
            city.focus();
            return;
        }

        const option = select.selectedOptions[0];
        setValue(city, option.dataset.city || '');
        setValue(street, option.dataset.street || '');
        fields.hidden = true;
    });
})();