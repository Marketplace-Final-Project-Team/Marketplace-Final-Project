(() => {
    const select = document.querySelector("[data-quantity-select]");
    if (!select) return;

    const inputs = Array.from(document.querySelectorAll("[data-quantity-input]"));

    const sync = () => {
        inputs.forEach((input) => { input.value = select.value; });
    };

    select.addEventListener("change", sync);
    sync();
})();
