/* =========================================================
   PWA — registro del Service Worker + botón "Instalar app"
   JS vanilla: no depende de jQuery ni del orden de carga.
   ========================================================= */
(function () {
    'use strict';

    var DISMISS_KEY = 'tas-install-dismissed';
    var DISMISS_DAYS = 14;
    var SHOW_DELAY_MS = 8000;
    var SCROLL_THRESHOLD_PX = 400;
    // Carrito y pedidos (checkout), cuenta (login) y admin: aquí no se ofrece instalar.
    var EXCLUDED_PREFIXES = ['/cart', '/orders', '/account', '/admin'];

    /* ---------- 1. Registro del Service Worker ---------- */
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js', { scope: '/' })
                .catch(function (error) {
                    console.warn('[PWA] No se pudo registrar el Service Worker:', error);
                });
        });
    }

    /* ---------- 2. ¿Ya está instalada? Entonces no se muestra nada ---------- */
    var isStandalone = window.matchMedia('(display-mode: standalone)').matches ||
        window.navigator.standalone === true;
    if (isStandalone) return;

    var root = document.getElementById('tas-install');
    var installBtn = document.getElementById('tas-install-btn');
    var closeBtn = document.getElementById('tas-install-close');
    var iosPanel = document.getElementById('tas-install-ios');
    if (!root || !installBtn || !closeBtn || !iosPanel) return;

    if (isExcludedPath() || wasDismissedRecently()) return;

    // iOS no tiene beforeinstallprompt: en Safari se muestran instrucciones.
    // (iPadOS se presenta como Mac, por eso se revisa también maxTouchPoints.)
    var ua = navigator.userAgent;
    var isIos = /iphone|ipad|ipod/i.test(ua) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    var isIosSafari = isIos && /safari/i.test(ua) && !/crios|fxios|edgios|opios/i.test(ua);

    var deferredPrompt = null;   // evento guardado de beforeinstallprompt (Chromium)
    var canInstall = isIosSafari;
    var timingReached = false;   // ~8 s en el sitio o primer scroll significativo
    var installed = false;

    /* ---------- 3. Chromium: esperar a que el navegador ofrezca instalar ---------- */
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        canInstall = true;
        maybeShow();
    });

    window.addEventListener('appinstalled', function () {
        installed = true;
        deferredPrompt = null;
        hide();
    });

    /* ---------- 6. Momento de aparición ---------- */
    var delayTimer = setTimeout(onTimingReached, SHOW_DELAY_MS);
    window.addEventListener('scroll', onScroll, { passive: true });

    function onScroll() {
        if (window.scrollY > SCROLL_THRESHOLD_PX) onTimingReached();
    }

    function onTimingReached() {
        if (timingReached) return;
        timingReached = true;
        clearTimeout(delayTimer);
        window.removeEventListener('scroll', onScroll);
        maybeShow();
    }

    /* ---------- Acciones del botón ---------- */
    installBtn.addEventListener('click', function () {
        if (deferredPrompt) {
            var promptEvent = deferredPrompt;
            deferredPrompt = null; // prompt() solo puede usarse una vez
            promptEvent.prompt();
            promptEvent.userChoice.finally(hide);
            return;
        }
        if (isIosSafari) toggleIosPanel();
    });

    // 5. Descartar: no volver a mostrar durante 14 días.
    closeBtn.addEventListener('click', function () {
        try {
            localStorage.setItem(DISMISS_KEY, String(Date.now()));
        } catch (e) { /* almacenamiento bloqueado: solo se oculta en esta visita */ }
        hide();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !iosPanel.hidden) toggleIosPanel(false);
    });

    /* ---------- Helpers ---------- */
    function maybeShow() {
        if (!canInstall || !timingReached || installed || !root.hidden) return;
        root.hidden = false;
        // Un frame después del display, para que corra la transición de entrada.
        requestAnimationFrame(function () {
            requestAnimationFrame(function () { root.classList.add('is-visible'); });
        });
    }

    function hide() {
        toggleIosPanel(false);
        root.classList.remove('is-visible');
        root.hidden = true;
        canInstall = false;
    }

    function toggleIosPanel(force) {
        var open = typeof force === 'boolean' ? force : iosPanel.hidden;
        iosPanel.hidden = !open;
        installBtn.setAttribute('aria-expanded', String(open));
    }

    function isExcludedPath() {
        var path = window.location.pathname.toLowerCase();
        return EXCLUDED_PREFIXES.some(function (prefix) {
            return path === prefix || path.indexOf(prefix + '/') === 0;
        });
    }

    function wasDismissedRecently() {
        try {
            var dismissedAt = parseInt(localStorage.getItem(DISMISS_KEY), 10);
            if (!dismissedAt) return false;
            return Date.now() - dismissedAt < DISMISS_DAYS * 24 * 60 * 60 * 1000;
        } catch (e) {
            return false;
        }
    }
})();
