/**
 * SERVICIOS - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

document.addEventListener('DOMContentLoaded', function () {
    initNavbarScroll();
    initAnimations();
    initMobileMenu();
});

/**
 * Menú móvil toggle (Reutilizado del Home)
 */
function initMobileMenu() {
    const toggle = document.getElementById('navToggle');
    const menu = document.querySelector('.nav-menu');

    if (!toggle || !menu) return;

    toggle.addEventListener('click', function () {
        menu.style.display = menu.style.display === 'flex' ? 'none' : 'flex';
    });

    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', function () {
            menu.style.display = 'none';
        });
    });
}

/**
 * Cambiar estilo navbar al hacer scroll (Reutilizado del Home)
 */
function initNavbarScroll() {
    const navbar = document.getElementById('mainNav');
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
 * Animaciones de entrada con IntersectionObserver
 */
function initAnimations() {
    if (!('IntersectionObserver' in window)) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    document.querySelectorAll('.servicios-route-block, .servicios-section-header, .servicios-filters').forEach(el => {
        observer.observe(el);
    });
}
