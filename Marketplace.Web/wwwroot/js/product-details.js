(() => {
    const mainImage = document.querySelector("[data-gallery-main]");
    const counter = document.querySelector("[data-gallery-counter]");
    const thumbnails = document.querySelectorAll("[data-gallery-thumb]");

    if (!mainImage || thumbnails.length === 0) {
        return;
    }

    thumbnails.forEach((thumbnail) => {
        thumbnail.addEventListener("click", () => {
            const imageUrl = thumbnail.dataset.imageUrl;
            const imageAlt = thumbnail.dataset.imageAlt;
            const imageIndex = thumbnail.dataset.imageIndex;

            if (!imageUrl) {
                return;
            }

            mainImage.src = imageUrl;
            mainImage.alt = imageAlt || mainImage.alt;

            thumbnails.forEach((item) => {
                const isSelected = item === thumbnail;
                item.classList.toggle("is-active", isSelected);
                item.setAttribute("aria-pressed", isSelected.toString());
            });

            if (counter && imageIndex) {
                counter.textContent = `${imageIndex} / ${thumbnails.length}`;
            }
        });
    });
})();
