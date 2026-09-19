(() => {
    const selector = document.querySelector("[data-variant-selector]");
    if (!selector) return;

    const options = Array.from(selector.querySelectorAll("[data-variant-option]"));
    const hint = selector.querySelector("[data-variant-hint]");
    const variantInputs = Array.from(document.querySelectorAll("[data-variant-input]"));
    const submitButtons = Array.from(document.querySelectorAll('[data-requires-variant="true"]'));

    const setSelected = (option) => {
        options.forEach((o) => o.classList.remove("is-selected"));
        option.classList.add("is-selected");

        const variantId = option.dataset.variantId;
        variantInputs.forEach((input) => { input.value = variantId; });
        submitButtons.forEach((button) => { button.disabled = false; });

        const stock = Number(option.dataset.variantStock ?? "0");
        if (hint) {
            hint.textContent = stock > 0 ? `Доступно: ${stock} шт.` : "Немає в наявності";
        }
    };

    submitButtons.forEach((button) => { button.disabled = true; });

    options.forEach((option) => {
        option.addEventListener("click", () => {
            if (option.disabled) return;
            setSelected(option);
        });
    });
})();
