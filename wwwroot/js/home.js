/**
 * HOME - PLATAFORMA OUTDOOR CINEMATOGRÁFICA
 * Funcionalidad de cliente para animaciones, interacciones y scroll effects
 */

document.addEventListener('DOMContentLoaded', function () {
    // === INICIALIZAR COMPONENTES ===
    initNavbarScroll();
    initAnimations();
    initSmoothScroll();
});

/**
 * Cambiar navbar al hacer scroll
 */
function initNavbarScroll() {
    const navbar = document.querySelector('.navbar-outdoor');
    
    if (!navbar) return;

    window.addEventListener('scroll', () => {
        if (window.pageYOffset > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    }, { passive: true });
}

/**
 * Inicializar animaciones de entrada
 */
function initAnimations() {
    const animatedElements = document.querySelectorAll('.home-animate');

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
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    animatedElements.forEach(el => {
        observer.observe(el);
    });
}

/**
 * Scroll suave hacia secciones
 */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            
            // Ignorar enlaces vacíos
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
}

/**
 * Parallax effect sutil en el hero (opcional)
 */
function initParallax() {
    const hero = document.querySelector('.hero-outdoor');
    
    if (!hero) return;

    window.addEventListener('scroll', () => {
        const scrolled = window.pageYOffset;
        const heroHeight = hero.offsetHeight;
        
        if (scrolled < heroHeight) {
            hero.style.transform = `translateY(${scrolled * 0.3}px)`;
        }
    }, { passive: true });
}

// Inicializar parallax
window.addEventListener('load', initParallax);

/**
 * Manejar clics en iconos navbar
 */
document.querySelectorAll('.navbar-icon').forEach(icon => {
    icon.addEventListener('click', function(e) {
        // Los links normales funcionan naturalmente
        // Los iconos especiales (#buscar, #favoritos, #perfil) pueden tener lógica adicional
        const href = this.getAttribute('href');
        
        if (href === '#buscar') {
            e.preventDefault();
            openSearchModal();
        } else if (href === '#favoritos') {
            e.preventDefault();
            openFavoritesModal();
        } else if (href === '#perfil') {
            e.preventDefault();
            openProfileMenu();
        }
    });
});

/**
 * Modal de búsqueda (placeholder)
 */
function openSearchModal() {
    console.log('Abriendo búsqueda global');
    // TODO: Implementar modal de búsqueda
}

/**
 * Modal de favoritos (placeholder)
 */
function openFavoritesModal() {
    console.log('Abriendo favoritos');
    // TODO: Implementar modal de favoritos
}

/**
 * Menú de perfil (placeholder)
 */
function openProfileMenu() {
    console.log('Abriendo perfil');
    // TODO: Implementar menú de perfil
}

/**
 * Exportar funciones para uso global
 */
window.homeModule = {
    openSearchModal,
    openFavoritesModal,
    openProfileMenu
};
