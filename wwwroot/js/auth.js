(function () {
    "use strict";

    // Alternar entre la vista de login y la de registro (fade simple)
    document.querySelectorAll("[data-auth-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var target = btn.getAttribute("data-auth-toggle"); // "signup" | "signin"
            var stack = btn.closest(".auth-page").querySelector(".auth-stack");
            if (!stack) return;
            var signIn = stack.querySelector('[data-view="signin"]');
            var signUp = stack.querySelector('[data-view="signup"]');
            if (target === "signup") {
                signIn.classList.add("is-hidden");
                signUp.classList.remove("is-hidden");
            } else {
                signUp.classList.add("is-hidden");
                signIn.classList.remove("is-hidden");
            }
        });
    });

    // Mostrar / ocultar contraseña
    document.querySelectorAll(".auth-toggle-visibility").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var input = document.getElementById(btn.getAttribute("data-target"));
            if (!input) return;
            input.type = input.type === "password" ? "text" : "password";
        });
    });

    // Estado de carga en el botón al enviar
    document.querySelectorAll(".auth-form").forEach(function (form) {
        form.addEventListener("submit", function () {
            if (form.checkValidity && !form.checkValidity()) return;
            var submitBtn = form.querySelector(".auth-submit");
            if (submitBtn) submitBtn.classList.add("is-loading");
        });
    });
})();