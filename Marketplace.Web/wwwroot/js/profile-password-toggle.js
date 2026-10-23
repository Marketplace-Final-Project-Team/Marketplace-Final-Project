(() => {
    document.querySelectorAll("[data-password-toggle]").forEach((toggle) => {
        toggle.addEventListener("click", () => {
            const wrap = toggle.closest(".profile-password-field");
            const input = wrap?.querySelector("[data-password-input]");
            if (!input) {
                return;
            }

            const show = input.type === "password";
            input.type = show ? "text" : "password";
            toggle.textContent = show ? __t("Сховати") : __t("Показати");
            toggle.setAttribute("aria-pressed", show.toString());
        });
    });
})();
