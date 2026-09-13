(() => {
    const widget = document.querySelector('[data-discounts]');
    if (!widget) {
        return;
    }

    let discounts;
    try {
        discounts = JSON.parse(widget.dataset.discounts);
    } catch {
        return;
    }

    if (!Array.isArray(discounts) || discounts.length <= 1) {
        return; 
    }

    const display = widget.querySelector('[data-discount-display]');
    if (!display) {
        return;
    }

    let currentIndex = 0;
    const rotationIntervalMs = 3500;
    const fadeDurationMs = 350;

    const renderDiscount = (item) => {
        display.innerHTML = `<strong>-${item.percent}%</strong> в «${item.store}»`;
    };

    setInterval(() => {
        display.classList.add('is-fading');

        setTimeout(() => {
            currentIndex = (currentIndex + 1) % discounts.length;
            renderDiscount(discounts[currentIndex]);
            display.classList.remove('is-fading');
        }, fadeDurationMs);
    }, rotationIntervalMs);
})();