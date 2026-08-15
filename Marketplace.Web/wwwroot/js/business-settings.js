(() => {
    const panel = document.querySelector("[data-local-settings]");
    if (!panel) {
        return;
    }

    const businessId = panel.dataset.businessId;
    const storageKey = `mp_business_local_settings_${businessId}`;

    const readSettings = () => {
        try {
            const raw = localStorage.getItem(storageKey);
            return raw ? JSON.parse(raw) : {};
        } catch {
            return {};
        }
    };

    const fields = Array.from(panel.querySelectorAll("[data-local-field]"));
    const saved = readSettings();

    fields.forEach((field) => {
        const key = field.dataset.localField;
        if (!(key in saved)) {
            return;
        }
        if (field.type === "checkbox") {
            field.checked = !!saved[key];
        } else {
            field.value = saved[key];
        }
    });

    const saveButton = panel.querySelector("[data-save-local-settings]");
    const savedHint = panel.querySelector("[data-local-saved-hint]");

    saveButton?.addEventListener("click", () => {
        const values = {};
        fields.forEach((field) => {
            values[field.dataset.localField] = field.type === "checkbox" ? field.checked : field.value.trim();
        });

        try {
            localStorage.setItem(storageKey, JSON.stringify(values));
        } catch {
            // localStorage unavailable — settings simply won't persist across reloads.
        }

        if (savedHint) {
            savedHint.hidden = false;
            setTimeout(() => {
                savedHint.hidden = true;
            }, 2000);
        }
    });
})();
