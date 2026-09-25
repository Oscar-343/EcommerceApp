/**
 * HOME - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

// El scroll y el menú móvil de la navbar ahora viven en site.js
// (initNavbarScroll / initMobileMenu), compartidos por todas las páginas.
document.addEventListener('DOMContentLoaded', function () {
    initSmoothScroll();
    initHeroVideo();
});

/**
 * Scroll suave hacia secciones
 */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            
            // Ignorar enlaces vacíos o especiales
            if (href === '#' || href === '#search' || href === '#favorites' || href === '#profile') {
                return;
            }
            
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
 * Inicializar y forzar reproducción del video hero
 */
function initHeroVideo() {
    const video = document.querySelector('.hero-brand__video');

    if (!video) {
        return;
    }

    // Configuración para autoplay
    video.muted = true;
    video.volume = 0;
    video.autoplay = true;
    video.loop = true;
    video.playsInline = true;

    // Intentar reproducir
    video.play().catch(function (error) {
        console.error('VIDEO: no se pudo iniciar autoplay', error);
    });
}
