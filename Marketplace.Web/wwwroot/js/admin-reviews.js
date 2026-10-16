(() => {
    const alertBox = document.querySelector("[data-admin-review-alert]");

    const showMessage = (message, isError) => {
        if (!alertBox) return;
        alertBox.classList.toggle("admin-alert--error", isError);
        alertBox.querySelector("span").textContent = isError ? "!" : "✓";
        alertBox.querySelector("p").textContent = message;
        alertBox.hidden = false;
        alertBox.scrollIntoView({ behavior: "smooth", block: "nearest" });
    };

    document.querySelectorAll("[data-toggle-form]").forEach((button) => {
        button.addEventListener("click", () => {
            const article = button.closest("article");
            if (!article) return;

            const target = button.dataset.toggleForm;
            article.querySelectorAll(".admin-review__reason-form").forEach((form) => {
                form.hidden = form.dataset.form === target ? !form.hidden : true;
                if (!form.hidden) form.querySelector("textarea")?.focus();
            });
        });
    });

    document.querySelectorAll("[data-admin-review-form]").forEach((form) => {
        form.addEventListener("submit", async (event) => {
            event.preventDefault();

            const confirmText = form.dataset.confirm;
            if (confirmText && !window.confirm(confirmText)) return;

            const submitButtons = form.querySelectorAll("button[type=submit]");
            submitButtons.forEach((b) => { b.disabled = true; });

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new FormData(form),
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                });
                const data = await response.json().catch(() => ({}));
                const message = data.message || (response.ok ? "Готово" : "Не вдалося виконати дію");

                showMessage(message, !response.ok);
                if (response.ok) {
                    setTimeout(() => window.location.reload(), 700);
                    return;
                }
            } catch {
                showMessage("Помилка з'єднання із сервером.", true);
            }

            submitButtons.forEach((b) => { b.disabled = false; });
        });
    });
})();
