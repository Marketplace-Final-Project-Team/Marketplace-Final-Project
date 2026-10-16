(function () {
    const filterDrawer = document.querySelector('[data-catalog-filter-open]');
    const filterSidebar = document.getElementById('catalogFilters');
    const filterBackdrop = document.querySelector('[data-catalog-filter-backdrop]');
    const filterClose = document.querySelector('[data-catalog-filter-close]');

    if (!filterDrawer || !filterSidebar || !filterBackdrop) {
        return;
    }

    function setFilterDrawer(open) {
        filterSidebar.classList.toggle('is-open', open);
        filterBackdrop.hidden = !open;
        filterDrawer.setAttribute('aria-expanded', open ? 'true' : 'false');
        document.body.classList.toggle('catalog-filters-open', open);

        if (open) {
            filterClose?.focus();
        } else {
            filterDrawer.focus();
        }
    }

    filterDrawer.addEventListener('click', () => setFilterDrawer(true));
    filterClose?.addEventListener('click', () => setFilterDrawer(false));
    filterBackdrop.addEventListener('click', () => setFilterDrawer(false));

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && filterSidebar.classList.contains('is-open')) {
            setFilterDrawer(false);
        }
    });

    window.addEventListener('resize', () => {
        if (window.innerWidth >= 992 && filterSidebar.classList.contains('is-open')) {
            setFilterDrawer(false);
        }
    });
})();