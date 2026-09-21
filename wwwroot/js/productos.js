/**
 * PRODUCTOS - Sistema de Equipamiento Outdoor
 * Funcionalidad de cliente para filtros, búsqueda y interacciones
 */

document.addEventListener('DOMContentLoaded', function () {
    // === INICIALIZAR COMPONENTES ===
    initFiltersToggle();
    initFavorites();
    initAnimations();
});

/**
 * Inicializar toggle de filtros en móvil
 */
function initFiltersToggle() {
    const filtrosToggle = document.getElementById('filtros-toggle');
    const filtros = document.getElementById('filtros');
    const overlay = document.getElementById('filtros-overlay');

    if (!filtrosToggle) return;

    filtrosToggle.addEventListener('click', function () {
        filtros.classList.toggle('active');
        overlay.classList.toggle('active');
    });

    overlay.addEventListener('click', function () {
        filtros.classList.remove('active');
        overlay.classList.remove('active');
    });

    // Cerrar filtros al hacer clic en un enlace
    const links = filtros.querySelectorAll('a');
    links.forEach(link => {
        link.addEventListener('click', function () {
            setTimeout(() => {
                filtros.classList.remove('active');
                overlay.classList.remove('active');
            }, 100);
        });
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
                    if (response.redirected) {
                        window.location.href = response.url;
                        return null;
                    }
                    return response.json();
                })
                .then(data => {
                    if (!data || !data.success) return;
                    boton.classList.toggle('active', data.isFavorite);
                    boton.innerHTML = data.isFavorite ? '♥' : '♡';
                });
        });
    });
}

/**
 * Agregar al carrito: POST real a Cart/Add.
 */
function agregarAlCarrito(productId) {
    const btn = event && event.target ? event.target : null;
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', 1);
    const token = getAntiForgeryToken();
    if (token) formData.append('__RequestVerificationToken', token);

    fetch('/Cart/Add', {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData
    })
        .then(response => {
            if (response.redirected) {
                window.location.href = response.url;
                return null;
            }
            return response.json();
        })
        .then(data => {
            if (!data) return;
            if (data.success && btn) {
                const originalText = btn.innerHTML;
                btn.innerHTML = '✓ AGREGADO';
                btn.style.backgroundColor = '#4F8063';
                setTimeout(() => {
                    btn.innerHTML = originalText;
                    btn.style.backgroundColor = '';
                }, 1500);
            } else if (!data.success) {
                alert(data.message || 'No se pudo agregar el producto.');
            }
        });
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
