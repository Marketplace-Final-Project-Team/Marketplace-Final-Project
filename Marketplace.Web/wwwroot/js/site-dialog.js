(() => {
    "use strict";

    if (window.siteDialog) {
        return;
    }

    const TEXTS = {
        uk: { ok: "OK", cancel: "Скасувати", confirm: "Підтвердити", error: "Помилка", success: "Готово", warning: "Увага", info: "Повідомлення", question: "Підтвердьте дію" },
        en: { ok: "OK", cancel: "Cancel", confirm: "Confirm", error: "Error", success: "Done", warning: "Warning", info: "Notice", question: "Please confirm" },
        de: { ok: "OK", cancel: "Abbrechen", confirm: "Bestätigen", error: "Fehler", success: "Fertig", warning: "Achtung", info: "Hinweis", question: "Bitte bestätigen" },
        fr: { ok: "OK", cancel: "Annuler", confirm: "Confirmer", error: "Erreur", success: "Terminé", warning: "Attention", info: "Information", question: "Veuillez confirmer" },
        es: { ok: "OK", cancel: "Cancelar", confirm: "Confirmar", error: "Error", success: "Listo", warning: "Atención", info: "Aviso", question: "Confirme la acción" }
    };

    // Статичні SVG (не містять користувацького вводу, тому innerHTML безпечний).
    const SVG_OPEN = '<svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">';
    const ICONS = {
        error: SVG_OPEN + '<circle cx="12" cy="12" r="9"/><path d="M12 7.5v5.5"/><path d="M12 16.5h.01"/></svg>',
        success: SVG_OPEN + '<circle cx="12" cy="12" r="9"/><path d="m8.5 12.5 2.5 2.5 4.5-5"/></svg>',
        warning: SVG_OPEN + '<path d="M10.3 4.3 2.7 17.5A2 2 0 0 0 4.4 20.5h15.2a2 2 0 0 0 1.7-3L13.7 4.3a2 2 0 0 0-3.4 0Z"/><path d="M12 9.5v4"/><path d="M12 17h.01"/></svg>',
        info: SVG_OPEN + '<circle cx="12" cy="12" r="9"/><path d="M12 11v5"/><path d="M12 7.5h.01"/></svg>',
        question: SVG_OPEN + '<circle cx="12" cy="12" r="9"/><path d="M9.6 9.5a2.4 2.4 0 1 1 3.4 2.2c-.6.3-1 .8-1 1.5"/><path d="M12 16.5h.01"/></svg>'
    };

    // Тип вікна для alert() визначаємо за початком тексту ("Помилка: ...").
    const ERROR_PREFIX = /^\s*(помилка|ошибка|error|fehler|erreur)(?=[\s:!.\-–—]|$)/i;
    const ERROR_STRIP = /^\s*(помилка|ошибка|error|fehler|erreur)\s*[:\-–—]\s*/i;
    const SUCCESS_PREFIX = /^\s*(успішно|готово|success|erfolg|succès|éxito)(?=[\s:!.\-–—]|$)/i;
    const SUCCESS_STRIP = /^\s*(успішно|готово|success|erfolg|succès|éxito)\s*[:\-–—]\s*/i;

    const lang = (document.documentElement.lang || "uk").slice(0, 2).toLowerCase();
    const t = (key) => (TEXTS[lang] || TEXTS.uk)[key];

    const queue = [];
    let active = false;

    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text; // textContent - без ризику XSS
        return node;
    }

    function normalize(kind, message, opts) {
        let type = opts.type;
        let text = String(message ?? "");

        if (!type && kind === "alert") {
            if (ERROR_PREFIX.test(text)) {
                type = "error";
                text = text.replace(ERROR_STRIP, "") || text;
            } else if (SUCCESS_PREFIX.test(text)) {
                type = "success";
                text = text.replace(SUCCESS_STRIP, "") || text;
            }
        }

        if (!type) {
            type = kind === "alert" ? "info" : (opts.danger ? "warning" : "question");
        }

        return {
            kind,
            type,
            text,
            title: opts.title || t(type),
            okText: opts.okText || (kind === "confirm" ? t("confirm") : t("ok")),
            cancelText: opts.cancelText || t("cancel"),
            danger: !!opts.danger,
            defaultValue: opts.defaultValue ?? "",
            placeholder: opts.placeholder || ""
        };
    }

    function show(kind, message, opts = {}) {
        return new Promise((resolve) => {
            queue.push({ config: normalize(kind, message, opts), resolve });
            if (!active) next();
        });
    }

    function next() {
        const item = queue.shift();
        if (!item) {
            active = false;
            document.documentElement.classList.remove("site-dialog-open");
            return;
        }
        active = true;
        document.documentElement.classList.add("site-dialog-open");
        render(item.config, item.resolve);
    }

    function render(config, resolve) {
        const previousFocus = document.activeElement;
        const isConfirm = config.kind === "confirm";
        const isPrompt = config.kind === "prompt";

        const overlay = el("div", "site-dialog-overlay");
        const dialog = el("div", `site-dialog site-dialog--${config.type}${isPrompt ? " site-dialog--prompt" : ""}`);
        dialog.setAttribute("role", "alertdialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "siteDialogTitle");
        dialog.setAttribute("aria-describedby", "siteDialogMessage");

        const icon = el("div", "site-dialog__icon");
        icon.innerHTML = ICONS[config.type];

        const title = el("h2", "site-dialog__title", config.title);
        title.id = "siteDialogTitle";
        const message = el("p", "site-dialog__message", config.text);
        message.id = "siteDialogMessage";

        let input = null;
        if (isPrompt) {
            input = el("input", "site-dialog__input");
            input.type = "text";
            input.value = config.defaultValue;
            input.placeholder = config.placeholder;
            input.setAttribute("aria-labelledby", "siteDialogMessage");
        }

        const actions = el("div", "site-dialog__actions");
        let cancelBtn = null;
        if (isConfirm || isPrompt) {
            cancelBtn = el("button", "site-dialog__btn site-dialog__btn--secondary", config.cancelText);
            cancelBtn.type = "button";
            actions.appendChild(cancelBtn);
        }
        const okBtn = el("button",
            `site-dialog__btn ${config.danger ? "site-dialog__btn--danger" : "site-dialog__btn--primary"}`,
            config.okText);
        okBtn.type = "button";
        actions.appendChild(okBtn);

        dialog.append(icon, title, message);
        if (input) dialog.appendChild(input);
        dialog.appendChild(actions);
        overlay.appendChild(dialog);

        // Якщо відкрита Bootstrap-модалка - вішаємо діалог усередину неї, інакше її
        // "пастка фокусу" забирає фокус у наших кнопок і клавіатура не працює.
        (document.querySelector(".modal.show") || document.body).appendChild(overlay);

        let closed = false;
        // Те, що повертає закриття без підтвердження (як у рідних вікон):
        // prompt -> null, confirm -> false, alert -> undefined.
        const cancelResult = isPrompt ? null : (isConfirm ? false : undefined);

        const close = (result) => {
            if (closed) return;
            closed = true;
            document.removeEventListener("keydown", onKeyDown, true);
            overlay.classList.remove("is-open");
            resolve(result);
            setTimeout(() => {
                overlay.remove();
                if (previousFocus && typeof previousFocus.focus === "function") {
                    previousFocus.focus();
                }
                next();
            }, 160);
        };

        const onKeyDown = (e) => {
            if (e.key === "Escape") {
                e.preventDefault();
                e.stopPropagation();
                close(cancelResult);
                return;
            }
            if (e.key === "Enter" && input && document.activeElement === input) {
                e.preventDefault();
                close(input.value);
                return;
            }
            if (e.key === "Tab") {
                const focusable = [input, cancelBtn, okBtn].filter(Boolean);
                const first = focusable[0];
                const last = focusable[focusable.length - 1];
                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };

        okBtn.addEventListener("click", () => close(isPrompt ? input.value : (isConfirm ? true : undefined)));
        if (cancelBtn) cancelBtn.addEventListener("click", () => close(cancelResult));
        overlay.addEventListener("mousedown", (e) => {
            if (e.target === overlay) close(cancelResult);
        });
        document.addEventListener("keydown", onKeyDown, true);

        requestAnimationFrame(() => {
            overlay.classList.add("is-open");
            if (input) {
                input.focus();
                input.select();
            } else {
                // Для небезпечних дій фокус одразу на "Скасувати", щоб випадковий Enter нічого не видалив.
                (config.danger && cancelBtn ? cancelBtn : okBtn).focus();
            }
        });
    }

    window.siteDialog = {
        alert: (message, opts) => show("alert", message, opts),
        confirm: (message, opts) => show("confirm", message, opts),
        prompt: (message, defaultValue, opts) => show("prompt", message, { ...opts, defaultValue })
    };

    // Усі наявні виклики alert(...) автоматично стають стильними.
    // УВАГА: на відміну від рідного alert, цей НЕ блокує код. Якщо одразу після alert
    // йде reload/redirect, використовуйте: await siteDialog.alert("...");
    // confirm/prompt підмінити так не можна (вони мають повертати значення синхронно):
    // для confirm використовуйте атрибут data-confirm або await siteDialog.confirm(...).
    window.alert = (message) => { window.siteDialog.alert(message); };

    // ---- Декларативне підтвердження: data-confirm ----
    // <form data-confirm="Видалити?">   <button data-confirm="..."> / <a data-confirm="...">
    // Додатково: data-confirm-title, data-confirm-ok, data-confirm-cancel,
    //            data-confirm-danger="true", data-confirm-type="warning"
    const optionsFrom = (node) => ({
        title: node.dataset.confirmTitle,
        okText: node.dataset.confirmOk,
        cancelText: node.dataset.confirmCancel,
        type: node.dataset.confirmType,
        danger: node.dataset.confirmDanger === "true"
    });

    // ---- Сумісність зі старою розміткою: onsubmit/onclick="return confirm('...')" ----
    // Перетворюється на data-confirm у момент події, ще до того, як спрацює вбудований
    // обробник, тому рідне вікно браузера більше не з'являється. Якщо вираз складніший
    // за простий "return confirm('...')", він лишається як є, а в консолі буде попередження.
    const INLINE_CONFIRM = /^\s*return\s+(?:window\.)?confirm\(\s*(['"])([\s\S]*)\1\s*\)\s*;?\s*$/;

    function upgradeInlineConfirm(node, attrName) {
        const code = node.getAttribute(attrName);
        const match = code && INLINE_CONFIRM.exec(code);
        if (!match) {
            if (code && /confirm\s*\(/.test(code)) {
                console.warn("site-dialog: не вдалося автоматично замінити confirm в", attrName, node);
            }
            return;
        }
        node.dataset.confirm = match[2].replace(/\\n/g, "\n").replace(/\\(['"])/g, "$1");
        node.removeAttribute(attrName);
    }

    document.addEventListener("click", async (e) => {
        const origin = e.target instanceof Element ? e.target : null;
        const legacy = origin ? origin.closest('[onclick*="confirm("]') : null;
        if (legacy) upgradeInlineConfirm(legacy, "onclick");

        const trigger = origin ? origin.closest("[data-confirm]") : null;
        if (!trigger || trigger.tagName === "FORM") return;

        if (trigger.dataset.confirmed === "1") {
            delete trigger.dataset.confirmed;
            return;
        }

        e.preventDefault();
        e.stopPropagation();

        if (await window.siteDialog.confirm(trigger.dataset.confirm, optionsFrom(trigger))) {
            trigger.dataset.confirmed = "1";
            trigger.click();
        }
    }, true);

    document.addEventListener("submit", async (e) => {
        const form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.hasAttribute("onsubmit")) upgradeInlineConfirm(form, "onsubmit");
        if (!form.hasAttribute("data-confirm")) return;

        if (form.dataset.confirmed === "1") {
            delete form.dataset.confirmed;
            return;
        }

        e.preventDefault();

        if (await window.siteDialog.confirm(form.dataset.confirm, optionsFrom(form))) {
            form.dataset.confirmed = "1";
            form.requestSubmit(e.submitter || undefined);
        }
    }, true);
})();