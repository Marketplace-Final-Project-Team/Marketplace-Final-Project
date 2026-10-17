(() => {
    const pad = value => String(value).padStart(2, "0");
    const toLocalInput = date =>
        `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
        `T${pad(date.getHours())}:${pad(date.getMinutes())}`;

    document.querySelectorAll("input[data-utc-input]").forEach(input => {
        if (input.dataset.utcValue && !input.value) {
            const date = new Date(input.dataset.utcValue);
            if (!Number.isNaN(date.getTime())) input.value = toLocalInput(date);
        }
        const validateLocalTime = () => {
            const date = new Date(input.value);
            const shifted = input.value && !Number.isNaN(date.getTime()) &&
                toLocalInput(date) !== input.value.slice(0, 16);
            input.setCustomValidity(shifted
                ? __t("Цей місцевий час не існує через переведення годинника. Оберіть інший час.")
                : "");
        };
        input.addEventListener("input", validateLocalTime);
        input.addEventListener("change", validateLocalTime);
        validateLocalTime();
    });

    // Preserve the visible local value and native form validation. Only the
    // submitted value is converted, using the offset for the selected date.
    document.querySelectorAll("form").forEach(form => {
        if (!form.querySelector("input[data-utc-input]")) return;
        form.addEventListener("formdata", event => {
            form.querySelectorAll("input[data-utc-input]").forEach(input => {
                if (!input.name || !input.value || input.disabled) return;
                const date = new Date(input.value);
                if (!Number.isNaN(date.getTime())) {
                    event.formData.set(input.name, date.toISOString());
                }
            });
        });
    });

    document.querySelectorAll("time[data-local-time]").forEach(element => {
        const date = new Date(element.dateTime);
        if (Number.isNaN(date.getTime())) return;
        let text = `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`;
        if (element.dataset.localTime !== "date") {
            text += ` ${pad(date.getHours())}:${pad(date.getMinutes())}`;
        }
        element.textContent = text;
    });
})();
