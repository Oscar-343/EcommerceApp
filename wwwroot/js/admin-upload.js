/* =========================================================
   ADMIN UPLOADS
   Las imágenes se envían junto con el formulario mediante
   multipart/form-data. No depende de AJAX para guardar.
   ========================================================= */

(function () {
    "use strict";

    const MAX_IMAGE = 5 * 1024 * 1024;
    const IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    function setStatus(id, message, type) {
        const el = document.getElementById(id);
        if (!el) return;
        el.textContent = message || "";
        el.className = "hint upload-status" + (type ? " " + type : "");
    }

    function previewUrlInput(inputId, previewId) {
        const input = document.getElementById(inputId);
        const box = document.getElementById(previewId);
        if (!input || !box) return;

        const render = () => {
            const url = input.value.trim();
            if (!url) {
                box.innerHTML = "<span>Vista previa</span>";
                return;
            }

            box.innerHTML = "";
            const img = document.createElement("img");
            img.src = url;
            img.alt = "Vista previa";
            img.loading = "lazy";
            img.onerror = () => {
                box.innerHTML = "<span>No se pudo cargar la imagen</span>";
            };
            box.appendChild(img);
        };

        input.addEventListener("input", render);
        render();
    }

    function wireFilePreview(fileInputId, urlInputId, previewId, statusId, multiple) {
        const fileInput = document.getElementById(fileInputId);
        const urlInput = document.getElementById(urlInputId);
        const preview = document.getElementById(previewId);

        if (!fileInput) return;

        fileInput.addEventListener("change", function () {
            const files = Array.from(fileInput.files || []);
            if (!files.length) {
                setStatus(statusId, "", "");
                return;
            }

            const invalid = files.find(file =>
                !IMAGE_TYPES.includes(file.type) || file.size > MAX_IMAGE
            );

            if (invalid) {
                const reason = !IMAGE_TYPES.includes(invalid.type)
                    ? "tipo no permitido"
                    : "supera los 5 MB";
                setStatus(statusId, `✕ ${invalid.name}: ${reason}.`, "error");
                fileInput.value = "";
                return;
            }

            setStatus(
                statusId,
                multiple
                    ? `✓ ${files.length} imagen(es) lista(s) para subir al guardar.`
                    : `✓ ${files[0].name} lista para subir al guardar.`,
                "success"
            );

            // Previsualización local sin subir todavía.
            if (preview && files[0]) {
                const reader = new FileReader();
                reader.onload = e => {
                    preview.innerHTML = "";
                    const img = document.createElement("img");
                    img.src = e.target.result;
                    img.alt = "Vista previa del archivo";
                    preview.appendChild(img);
                };
                reader.readAsDataURL(files[0]);
            }

            if (urlInput && !multiple) {
                urlInput.dataset.fileSelected = "true";
            }
        });
    }

    function wireForm(form) {
        if (!form) return;

        form.addEventListener("submit", function () {
            const button = form.querySelector('button[type="submit"]');
            if (button) {
                button.disabled = true;
                button.classList.add("is-loading");
                button.dataset.originalText = button.textContent.trim();
                button.textContent = "Guardando y subiendo…";
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        previewUrlInput("mainImg", "mainImgPreview");
        previewUrlInput("secImg", "secImgPreview");

        wireFilePreview(
            "mainImgFile",
            "mainImg",
            "mainImgPreview",
            "mainImgFileStatus",
            false
        );

        wireFilePreview(
            "secImgFile",
            "secImg",
            "secImgPreview",
            "secImgFileStatus",
            false
        );

        wireFilePreview(
            "galleryImgFile",
            "galleryImg",
            null,
            "galleryImgFileStatus",
            true
        );

        document.querySelectorAll(".admin-form").forEach(wireForm);
    });
})();
