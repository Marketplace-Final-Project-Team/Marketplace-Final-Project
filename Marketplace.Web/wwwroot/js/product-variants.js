(() => {
    const section = document.querySelector("[data-variants-section]");
    if (!section) return;

    const rowsContainer = section.querySelector("[data-variant-rows]");
    const addButton = section.querySelector("[data-add-variant]");
    const quantityField = document.querySelector("[data-quantity-field]");

    const buildRow = (index) => {
        const row = document.createElement("div");
        row.className = "product-variant-row";
        row.setAttribute("data-variant-row", "");
        row.innerHTML = `
            <input name="Variants[${index}].Size" class="form-control" placeholder="${__t("Розмір (S, M, L...)")}" />
            <input name="Variants[${index}].Sku" class="form-control" placeholder="${__t("Артикул розміру")}" />
            <input name="Variants[${index}].Quantity" type="number" min="0" value="0" class="form-control" placeholder="${__t("К-сть")}" />
            <button type="button" class="product-variant-row__remove" data-remove-variant aria-label="${__t("Видалити розмір")}">✕</button>
        `;
        return row;
    };

    const reindexRows = () => {
        rowsContainer.querySelectorAll("[data-variant-row]").forEach((row, index) => {
            row.querySelectorAll("input").forEach((input) => {
                input.name = input.name.replace(/Variants\[\d+\]/, `Variants[${index}]`);
            });
        });
        toggleQuantityField();
    };

    const toggleQuantityField = () => {
        if (!quantityField) return;
        const hasRows = rowsContainer.querySelectorAll("[data-variant-row]").length > 0;
        quantityField.hidden = hasRows;
        const input = quantityField.querySelector("input");
        if (input) input.disabled = hasRows;
    };

    addButton.addEventListener("click", () => {
        const index = rowsContainer.querySelectorAll("[data-variant-row]").length;
        rowsContainer.appendChild(buildRow(index));
        toggleQuantityField();
    });

    rowsContainer.addEventListener("click", (event) => {
        const removeButton = event.target.closest("[data-remove-variant]");
        if (!removeButton) return;
        removeButton.closest("[data-variant-row]")?.remove();
        reindexRows();
    });

    toggleQuantityField();
})();
