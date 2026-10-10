(function () {
    const root = document.querySelector('[data-ai-chat]');
    if (!root) return;

    const toggle = root.querySelector('[data-ai-chat-toggle]');
    const closeBtn = root.querySelector('[data-ai-chat-close]');
    const form = root.querySelector('[data-ai-chat-form]');
    const input = root.querySelector('[data-ai-chat-input]');
    const sendBtn = root.querySelector('[data-ai-chat-send]');
    const messages = root.querySelector('[data-ai-chat-messages]');
    const typing = root.querySelector('[data-ai-chat-typing]');

    let isSending = false;

    function escapeHtml(text) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return text.replace(/[&<>"']/g, (m) => map[m]);
    }

    // Renders a small, safe subset of markdown (**bold** and "- " bullet lists).
    // Everything is HTML-escaped first, so the only markup that can ever reach
    // innerHTML is the <strong>/<ul>/<li>/<p> wrappers this function itself adds.
    function formatBotText(text) {
        const inline = (line) => escapeHtml(line).replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        const blocks = text.split(/\n{2,}/);
        return blocks.map((block) => {
            const lines = block.split('\n').filter((l) => l.trim().length > 0);
            const isList = lines.length > 0 && lines.every((l) => /^[-*]\s+/.test(l.trim()));
            if (isList) {
                const items = lines.map((l) => `<li>${inline(l.trim().replace(/^[-*]\s+/, ''))}</li>`).join('');
                return `<ul>${items}</ul>`;
            }
            return `<p>${lines.map(inline).join('<br>')}</p>`;
        }).join('');
    }

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function appendMessage(text, role) {
        const wrap = document.createElement('div');
        wrap.className = `ai-chat__message ai-chat__message--${role}`;

        if (role === 'bot' || role === 'error') {
            const avatar = document.createElement('span');
            avatar.className = 'ai-chat__message-avatar';
            avatar.setAttribute('aria-hidden', 'true');
            avatar.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" width="13" height="13"><path d="M12 2 9.5 7 4 8l4 4-1 6 5-3 5 3-1-6 4-4-5.5-1z"/></svg>';
            wrap.appendChild(avatar);
        }

        const bubble = document.createElement('div');
        bubble.className = 'ai-chat__bubble';
        bubble.innerHTML = role === 'bot' ? formatBotText(text) : escapeHtml(text);
        wrap.appendChild(bubble);

        messages.appendChild(wrap);
        scrollToBottom();
    }

    function setOpen(open) {
        root.classList.toggle('is-open', open);
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        if (open) {
            window.setTimeout(() => input.focus(), 160);
        }
    }

    function autoResize() {
        input.style.height = 'auto';
        input.style.height = `${input.scrollHeight}px`;
    }

    toggle.addEventListener('click', () => setOpen(!root.classList.contains('is-open')));
    closeBtn.addEventListener('click', () => setOpen(false));

    input.addEventListener('input', autoResize);
    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            form.requestSubmit();
        }
    });

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const text = input.value.trim();
        if (!text || isSending) return;

        appendMessage(text, 'user');
        input.value = '';
        autoResize();

        isSending = true;
        sendBtn.disabled = true;
        typing.hidden = false;
        scrollToBottom();

        try {
            const response = await fetch('/api/chat/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text })
            });

            const data = await response.json().catch(() => null);

            if (response.ok && data && typeof data.message === 'string') {
                appendMessage(data.message, 'bot');
            } else if (response.status === 401) {
                appendMessage('Сесія закінчилася. Оновіть сторінку та увійдіть знову.', 'error');
            } else {
                appendMessage((data && data.error) || 'Щось пішло не так. Спробуйте ще раз.', 'error');
            }
        } catch {
            appendMessage('Не вдалося з’єднатися з сервером. Перевірте інтернет-з’єднання.', 'error');
        } finally {
            typing.hidden = true;
            isSending = false;
            sendBtn.disabled = false;
        }
    });
})();
