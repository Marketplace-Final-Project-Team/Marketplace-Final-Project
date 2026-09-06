(() => {
    // Google Translate is the first externally-hosted script in this project, so
    // it's gated behind the same first-party cookie-consent flow that already
    // protects RecentlyViewedCookie - it only loads once the visitor has accepted.
    const CONSENT_COOKIE = "mp_cookie_consent";

    function isConsentAccepted() {
        const match = document.cookie.match(new RegExp(`(?:^|; )${CONSENT_COOKIE}=([^;]*)`));
        return match ? decodeURIComponent(match[1]) === "accepted" : false;
    }

    let translateRequested = false;

    function loadGoogleTranslate() {
        if (translateRequested || !document.getElementById("google_translate_element")) {
            return;
        }
        translateRequested = true;

        window.googleTranslateElementInit = function () {
            // eslint-disable-next-line no-undef
            new google.translate.TranslateElement(
                {
                    pageLanguage: "uk",
                    includedLanguages: "en,uk,pl,de,fr,es",
                    layout: google.translate.TranslateElement.InlineLayout.SIMPLE,
                    autoDisplay: false
                },
                "google_translate_element"
            );
        };

        const script = document.createElement("script");
        script.src = "https://translate.google.com/translate_a/element.js?cb=googleTranslateElementInit";
        script.async = true;
        document.body.appendChild(script);
    }

    if (isConsentAccepted()) {
        loadGoogleTranslate();
    }

    document.querySelector("[data-cookie-accept]")?.addEventListener("click", () => {
        setTimeout(loadGoogleTranslate, 50);
    });
})();
