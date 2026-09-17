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
 * Inicializar funcionalidad de favoritos
 */
function initFavorites() {
    const favoriteButtons = document.querySelectorAll('.producto-card__favorite');

    favoriteButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const productId = this.getAttribute('data-product-id');
            this.classList.toggle('active');
            this.innerHTML = this.classList.contains('active') ? '♥' : '♡';

            // Guardar en localStorage (persistencia local)
            saveFavorite(productId, this.classList.contains('active'));

            // TODO: Sincronizar con servidor cuando esté implementado
        });

        // Restaurar favoritos desde localStorage
        const productId = btn.getAttribute('data-product-id');
        if (isFavorite(productId)) {
            btn.classList.add('active');
            btn.innerHTML = '♥';
        }
    });
}

/**
 * Guardar favorito en localStorage
 */
function saveFavorite(productId, isFav) {
    const favorites = JSON.parse(localStorage.getItem('productos-favoritos') || '[]');

    if (isFav) {
        if (!favorites.includes(productId)) {
            favorites.push(productId);
        }
    } else {
        const index = favorites.indexOf(productId);
        if (index > -1) {
            favorites.splice(index, 1);
        }
    }

    localStorage.setItem('productos-favoritos', JSON.stringify(favorites));
}

/**
 * Verificar si un producto es favorito
 */
function isFavorite(productId) {
    const favorites = JSON.parse(localStorage.getItem('productos-favoritos') || '[]');
    return favorites.includes(productId);
}

/**
 * Agregar al carrito (placeholder)
 */
function agregarAlCarrito(productId) {
    if (event && event.target) {
        const btn = event.target;
        const originalText = btn.innerHTML;
        btn.innerHTML = '✓ AGREGADO';
        btn.style.backgroundColor = '#4F8063';

        setTimeout(() => {
            btn.innerHTML = originalText;
            btn.style.backgroundColor = '';
        }, 1500);
    }

    // TODO: Implementar lógica real del carrito
    console.log('Producto agregado al carrito:', productId);
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
    },
    isFavorite,
    saveFavorite
};
