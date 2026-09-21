/**
 * SERVICIOS - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

// El scroll y el menú móvil de la navbar ahora viven en site.js,
// compartidos por todas las páginas.
document.addEventListener('DOMContentLoaded', function () {
    initAnimations();
});

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
