/**
 * PRODUCTOS - Sistema de Equipamiento Outdoor
 * Funcionalidad de cliente para filtros, búsqueda y interacciones
 */

document.addEventListener('DOMContentLoaded', function () {
    // === INICIALIZAR COMPONENTES ===
    initFiltersToggle();
    initFavorites();
    initAnimations();
    initQtyStepper();
});

/**
 * Stepper +/- de cantidad en cada tarjeta de producto (no llama al servidor,
 * solo ajusta el número local que se manda al agregar al carrito).
 */
function initQtyStepper() {
    document.querySelectorAll('.pcard__qty').forEach(function (qtyBox) {
        // Tope: el stock disponible, y como máximo 100 (el mismo límite que el carrito).
        const maxStock = Math.max(1, Math.min(parseInt(qtyBox.dataset.maxStock, 10) || 1, 100));
        const valueEl = qtyBox.querySelector('[data-qty-value]');
        const menos = qtyBox.querySelector('[data-qty-action="dec"]');
        const mas = qtyBox.querySelector('[data-qty-action="inc"]');

        function pintar(valor) {
            valueEl.textContent = valor;
            menos.disabled = valor <= 1;
            mas.disabled = valor >= maxStock;
        }

        menos.addEventListener('click', function () {
            pintar(Math.max((parseInt(valueEl.textContent, 10) || 1) - 1, 1));
        });
        mas.addEventListener('click', function () {
            pintar(Math.min((parseInt(valueEl.textContent, 10) || 1) + 1, maxStock));
        });

        pintar(1);
    });
}

/**
 * Panel de filtros en móvil y tablet (menos de 1200px). Usa clases
 * (is-open), nunca estilos en línea: desde 1200px el CSS lo muestra
 * como barra lateral fija y el estado móvil se limpia solo.
 */
function initFiltersToggle() {
    const toggle = document.getElementById('filtros-toggle');
    const filtros = document.getElementById('filtros');
    const overlay = document.getElementById('filtros-overlay');
    const cerrar = document.getElementById('filtros-close');
    if (!toggle || !filtros || !overlay) return;

    const escritorio = window.matchMedia('(min-width: 1200px)');

    function mostrar(abierto) {
        filtros.classList.toggle('is-open', abierto);
        overlay.classList.toggle('is-open', abierto);
        document.body.classList.toggle('filtros-open', abierto); // bloquea el scroll de atrás
        toggle.setAttribute('aria-expanded', String(abierto));
    }

    toggle.addEventListener('click', () => mostrar(true));
    overlay.addEventListener('click', () => mostrar(false));
    if (cerrar) cerrar.addEventListener('click', () => mostrar(false));

    // "Limpiar filtros" navega a otra página: se cierra antes de irse.
    filtros.querySelectorAll('a').forEach(link => link.addEventListener('click', () => mostrar(false)));

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && filtros.classList.contains('is-open')) mostrar(false);
    });

    escritorio.addEventListener('change', function (e) {
        if (e.matches) mostrar(false);
    });
}

/**
 * Token antiforgery presente en la página (@Html.AntiForgeryToken() en Products/Index).
 */
function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : null;
}

/**
 * Sin sesión, ASP.NET Identity responde 401 (en vez de redirigir) cuando la
 * petición manda X-Requested-With: XMLHttpRequest, así que hay que revisar
 * el status manualmente y mandar al login.
 */
function redirectToLogin() {
    window.location.href = '/Account/Login?ReturnUrl=' +
        encodeURIComponent(window.location.pathname);
}

/**
 * Inicializar funcionalidad de favoritos (sincronizado con el servidor vía Favorites/Toggle)
 */
function initFavorites() {
    const favoriteButtons = document.querySelectorAll('.producto-card__favorite');

    favoriteButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const productId = this.getAttribute('data-product-id');
            const boton = this;
            const formData = new FormData();
            formData.append('type', 'Product');
            formData.append('itemId', productId);
            const token = getAntiForgeryToken();
            if (token) formData.append('__RequestVerificationToken', token);

            fetch('/Favorites/Toggle', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            })
                .then(response => {
                    if (response.status === 401) {
                        redirectToLogin();
                        return null;
                    }
                    if (response.redirected) {
                        window.location.href = response.url;
                        return null;
                    }
                    return response.json();
                })
                .then(data => {
                    if (!data || !data.success) return;
                    boton.classList.toggle('active', data.isFavorite);
                    boton.setAttribute('aria-label', data.isFavorite ? 'Quitar de favoritos' : 'Guardar en favoritos');
                    const icono = boton.querySelector('i');
                    if (icono) {
                        icono.classList.toggle('fa-solid', data.isFavorite);
                        icono.classList.toggle('fa-regular', !data.isFavorite);
                    }
                });
        });
    });
}

/**
 * Agregar al carrito: POST real a Cart/Add, usando la cantidad elegida
 * con el stepper +/- de la propia tarjeta (por defecto 1 si no hay stepper).
 */
function agregarAlCarritoConCantidad(btn, productId) {
    const card = btn.closest('.pcard, .producto-card');
    const qtyEl = card ? card.querySelector('[data-qty-value]') : null;
    const quantity = qtyEl ? (parseInt(qtyEl.textContent, 10) || 1) : 1;

    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', quantity);
    const token = getAntiForgeryToken();
    if (token) formData.append('__RequestVerificationToken', token);

    fetch('/Cart/Add', {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData
    })
        .then(response => {
            if (response.status === 401) {
                redirectToLogin();
                return null;
            }
            if (response.redirected) {
                window.location.href = response.url;
                return null;
            }
            return response.json();
        })
        .then(data => {
            if (!data) return;
            if (data.success && btn) {
                // Confirmación breve: el ícono del carrito pasa a un check verde.
                const originalHtml = btn.innerHTML;
                btn.innerHTML = '<i class="fa-solid fa-check" aria-hidden="true"></i>';
                btn.classList.add('is-added');
                setTimeout(() => {
                    btn.innerHTML = originalHtml;
                    btn.classList.remove('is-added');
                }, 1500);
            } else if (!data.success) {
                alert(data.message || 'No se pudo agregar el producto.');
            }
        });
}

// Se mantiene por compatibilidad si algo externo todavía llama a este nombre.
function agregarAlCarrito(productId) {
    agregarAlCarritoConCantidad(event && event.target, productId);
}

/**
 * Inicializar animaciones de entrada
 */
function initAnimations() {
    const animatedElements = document.querySelectorAll('.productos-animate');

    if (!('IntersectionObserver' in window)) {
        // Fallback para navegadores antiguos
        animatedElements.forEach(el => el.style.opacity = '1');
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1
    });

    animatedElements.forEach(el => {
        observer.observe(el);
    });
}

/**
 * Scroll suave hacia secciones
 */
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href === '#') return;

        const target = document.querySelector(href);
        if (target) {
            e.preventDefault();
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

/**
 * Exportar funciones para uso global
 */
window.productosModule = {
    agregarAlCarrito,
    toggleFavorito: function (productId) {
        const btn = document.querySelector(`[data-product-id="${productId}"][class*="favorite"]`);
        if (btn) {
            btn.click();
        }
    }
};
