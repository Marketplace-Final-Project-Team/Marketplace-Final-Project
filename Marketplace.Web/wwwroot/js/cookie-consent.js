(() => {
    const modal = document.querySelector("[data-cookie-consent]");
    if (!modal) {
        return;
    }

    const COOKIE_NAME = "mp_cookie_consent";

    const getCookie = (name) => {
        const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
        return match ? decodeURIComponent(match[1]) : null;
    };

    const setCookie = (name, value, days) => {
        const expires = new Date(Date.now() + days * 24 * 60 * 60 * 1000).toUTCString();
        document.cookie = `${name}=${value}; expires=${expires}; path=/; SameSite=Lax`;
    };

    const openModal = () => {
        modal.hidden = false;
    };

    const closeModal = () => {
        modal.hidden = true;
    };

    if (!getCookie(COOKIE_NAME)) {
        openModal();
    }

    document.querySelector("[data-cookie-accept]")?.addEventListener("click", () => {
        setCookie(COOKIE_NAME, "accepted", 365);
        closeModal();
    });

    document.querySelector("[data-cookie-reject]")?.addEventListener("click", () => {
        setCookie(COOKIE_NAME, "rejected", 365);
        closeModal();
    });

    document.querySelectorAll("[data-reopen-cookie-consent]").forEach((trigger) => {
        trigger.addEventListener("click", (event) => {
            event.preventDefault();
            openModal();
        });
    });
})();
