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
            toggle.textContent = shouldShow ? "Сховати" : "Показати";
            toggle.setAttribute("aria-label", shouldShow ? "Сховати пароль" : "Показати пароль");
            toggle.setAttribute("aria-pressed", shouldShow.toString());
        });
    });
})();
