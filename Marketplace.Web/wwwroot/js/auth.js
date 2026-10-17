(() => {
    const toggles = document.querySelectorAll("[data-password-toggle]");

    toggles.forEach((toggle) => {
        toggle.addEventListener("click", () => {
            const field = toggle.closest(".field");
            const input = field?.querySelector("[data-password-input]");

            if (!input) {
                return;
            }

            const shouldShow = input.type === "password";
            input.type = shouldShow ? "text" : "password";
            toggle.textContent = shouldShow ? __t("Сховати") : __t("Показати");
            toggle.setAttribute("aria-label", shouldShow ? __t("Сховати пароль") : __t("Показати пароль"));
            toggle.setAttribute("aria-pressed", shouldShow.toString());
        });
    });

    const strengthInputs = document.querySelectorAll("[data-password-strength]");

    const calculateStrength = (password) => {
        if (!password) return { score: 0, label: __t("Не вказано"), state: "empty" };

        let score = 0;
        if (password.length >= 8) score += 1;
        if (password.length >= 12) score += 1;
        if (/[a-zа-яіїєґ]/u.test(password) && /[A-ZА-ЯІЇЄҐ]/u.test(password)) score += 1;
        if (/\d/.test(password)) score += 1;
        if (/[^\p{L}\p{N}\s]/u.test(password)) score += 1;

        if (score <= 2) return { score: 1, label: __t("Слабкий"), state: "weak" };
        if (score <= 4) return { score: 2, label: __t("Середній"), state: "medium" };
        return { score: 3, label: __t("Надійний"), state: "strong" };
    };

    strengthInputs.forEach((input) => {
        const field = input.closest(".field");
        const meter = field?.querySelector("[data-password-strength-meter]");
        const label = meter?.querySelector("[data-password-strength-label]");
        if (!meter || !label) return;

        const updateStrength = () => {
            const result = calculateStrength(input.value);
            meter.dataset.strength = result.state;
            meter.style.setProperty("--password-strength", `${(result.score / 3) * 100}%`);
            label.textContent = result.label;
        };

        input.addEventListener("input", updateStrength);
        updateStrength();
    });
})();
