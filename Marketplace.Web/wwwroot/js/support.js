(() => {
    document.querySelectorAll(".support-faq-item__question").forEach((button) => {
        button.addEventListener("click", () => {
            const item = button.closest(".support-faq-item");
            const answer = item.querySelector(".support-faq-item__answer");
            const isOpen = item.classList.toggle("is-open");
            button.setAttribute("aria-expanded", isOpen ? "true" : "false");
            if (answer) {
                answer.hidden = !isOpen;
            }
        });
    });

    const chatStub = document.querySelector("[data-chat-stub]");
    const openButtons = document.querySelectorAll("[data-open-chat-stub]");
    const closeButton = document.querySelector("[data-close-chat-stub]");

    if (chatStub) {
        openButtons.forEach((button) => {
            button.addEventListener("click", () => {
                chatStub.hidden = false;
            });
        });

        closeButton?.addEventListener("click", () => {
            chatStub.hidden = true;
        });

        chatStub.addEventListener("click", (event) => {
            if (event.target === chatStub) {
                chatStub.hidden = true;
            }
        });
    }
})();
