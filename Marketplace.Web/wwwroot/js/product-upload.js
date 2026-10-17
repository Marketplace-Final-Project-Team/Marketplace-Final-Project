(() => {
    const uploader = document.querySelector("[data-product-uploader]");
    if (!uploader) return;

    const MAX_FILE_SIZE = 5 * 1024 * 1024;
    const MAX_ADDITIONAL_FILES = 8;
    const ALLOWED_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
    const ALLOWED_EXTENSIONS = new Set(["jpg", "jpeg", "png", "webp"]);

    const formatBytes = (bytes) => {
        if (bytes < 1024 * 1024) return __t("{0} КБ", Math.max(1, Math.round(bytes / 1024)));
        return __t("{0} МБ", (bytes / (1024 * 1024)).toFixed(1).replace(".0", ""));
    };

    const fileKey = (file) => `${file.name}:${file.size}:${file.lastModified}`;

    const validateFile = (file) => {
        const extension = file.name.split(".").pop()?.toLowerCase() ?? "";
        if (!ALLOWED_TYPES.has(file.type) || !ALLOWED_EXTENSIONS.has(extension)) {
            return __t("«{0}» має непідтримуваний формат. Оберіть JPG, PNG або WEBP.", file.name);
        }
        if (file.size === 0) return __t("«{0}» порожній і не може бути завантажений.", file.name);
        if (file.size > MAX_FILE_SIZE) return __t("«{0}» завеликий ({1}). Максимум - 5 МБ.", file.name, formatBytes(file.size));
        return null;
    };

    const syncInputFiles = (input, files) => {
        const transfer = new DataTransfer();
        files.forEach((file) => transfer.items.add(file));
        input.files = transfer.files;
    };

    uploader.querySelectorAll("[data-upload-group]").forEach((group) => {
        const input = group.querySelector("[data-file-input]");
        const dropzone = group.querySelector("[data-dropzone]");
        const list = group.querySelector("[data-file-list]");
        const error = group.querySelector("[data-upload-error]");
        const counter = group.querySelector("[data-file-counter]");
        const isMultiple = group.dataset.uploadGroup === "additional";
        const state = { files: [], objectUrls: new Map() };

        if (!input || !dropzone || !list || !error) return;

        const setError = (messages = []) => {
            error.replaceChildren();
            messages.forEach((message) => {
                const item = document.createElement("p");
                item.textContent = message;
                error.append(item);
            });
            error.classList.toggle("is-visible", messages.length > 0);
            dropzone.classList.toggle("has-error", messages.length > 0);
        };

        const updateCounter = () => {
            if (counter) counter.textContent = __t("{0} / {1} файлів", state.files.length, MAX_ADDITIONAL_FILES);
        };

        const revokeUnusedUrls = () => {
            const activeKeys = new Set(state.files.map(fileKey));
            state.objectUrls.forEach((url, key) => {
                if (!activeKeys.has(key)) {
                    URL.revokeObjectURL(url);
                    state.objectUrls.delete(key);
                }
            });
        };

        const render = () => {
            revokeUnusedUrls();
            list.replaceChildren();

            state.files.forEach((file, index) => {
                const key = fileKey(file);
                const objectUrl = state.objectUrls.get(key) ?? URL.createObjectURL(file);
                state.objectUrls.set(key, objectUrl);

                const card = document.createElement("article");
                card.className = "product-file-card";

                const image = document.createElement("img");
                image.src = objectUrl;
                image.alt = __t("Попередній перегляд {0}", file.name);

                const information = document.createElement("div");
                information.className = "product-file-card__info";

                const role = document.createElement("span");
                role.className = "product-file-card__role";
                role.textContent = isMultiple ? __t("Фото {0}", index + 1) : __t("Головне фото");

                const name = document.createElement("strong");
                name.title = file.name;
                name.textContent = file.name;

                const size = document.createElement("small");
                size.textContent = __t("{0} · готове до завантаження", formatBytes(file.size));

                const remove = document.createElement("button");
                remove.type = "button";
                remove.className = "product-file-card__remove";
                remove.dataset.removeFile = index.toString();
                remove.setAttribute("aria-label", __t("Прибрати {0}", file.name));
                remove.innerHTML = "<span aria-hidden=\"true\">×</span>";

                information.append(role, name, size);
                card.append(image, information, remove);
                list.append(card);
            });

            syncInputFiles(input, state.files);
            updateCounter();
            dropzone.classList.toggle("has-files", state.files.length > 0);
        };

        const addFiles = (incomingFiles) => {
            const messages = [];
            const validFiles = [];

            Array.from(incomingFiles).forEach((file) => {
                const validationMessage = validateFile(file);
                if (validationMessage) messages.push(validationMessage);
                else validFiles.push(file);
            });

            if (isMultiple) {
                const existingKeys = new Set(state.files.map(fileKey));
                const uniqueFiles = validFiles.filter((file) => !existingKeys.has(fileKey(file)));
                const availableSlots = MAX_ADDITIONAL_FILES - state.files.length;
                if (uniqueFiles.length > availableSlots) {
                    messages.push(__t("Можна додати не більше {0} додаткових фото.", MAX_ADDITIONAL_FILES));
                }
                state.files.push(...uniqueFiles.slice(0, Math.max(0, availableSlots)));
            } else {
                state.files = validFiles.slice(0, 1);
                if (validFiles.length > 1) messages.push(__t("Для головного фото можна обрати лише один файл."));
            }

            setError(messages);
            render();
        };

        input.addEventListener("change", () => addFiles(input.files));

        dropzone.addEventListener("click", (event) => {
            if (event.target !== input) input.click();
        });
        ["dragenter", "dragover"].forEach((eventName) => {
            dropzone.addEventListener(eventName, (event) => {
                event.preventDefault();
                dropzone.classList.add("is-dragging");
            });
        });

        ["dragleave", "drop"].forEach((eventName) => {
            dropzone.addEventListener(eventName, (event) => {
                event.preventDefault();
                dropzone.classList.remove("is-dragging");
            });
        });

        dropzone.addEventListener("drop", (event) => addFiles(event.dataTransfer?.files ?? []));

        list.addEventListener("click", (event) => {
            const button = event.target.closest("[data-remove-file]");
            if (!button) return;

            const index = Number(button.dataset.removeFile);
            if (!Number.isInteger(index)) return;

            state.files.splice(index, 1);
            setError();
            render();
            input.focus();
        });

        window.addEventListener("beforeunload", () => {
            state.objectUrls.forEach((url) => URL.revokeObjectURL(url));
        });

        render();
    });
})();
