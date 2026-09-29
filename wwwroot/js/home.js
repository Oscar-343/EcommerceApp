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
 * Video del hero SOLO en laptop/escritorio (992px o más).
 * En celular y tablet no se descarga: se queda la foto (evita tirones y gasto de datos).
 * Tampoco se carga si el usuario pidió ahorrar datos o reducir animaciones.
 */
function initHeroVideo() {
    const video = document.querySelector('.hero-brand__video');
    if (!video || !video.dataset.src) return;

    const escritorio = window.matchMedia('(min-width: 992px)');
    const menosMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const ahorroDatos = navigator.connection && navigator.connection.saveData;

    function cargarVideo() {
        if (!escritorio.matches || menosMovimiento || ahorroDatos || video.dataset.cargado) return;

        video.dataset.cargado = 'true';
        video.muted = true;
        video.src = video.dataset.src;
        video.play().catch(function () {
            // Si el navegador bloquea el autoplay, se queda la foto de fondo.
        });
    }

    cargarVideo();
    // Si la ventana se agranda (por ejemplo, de tablet a pantalla completa), se carga en ese momento.
    escritorio.addEventListener('change', cargarVideo);

    // Si la ventana se achica a tamaño celular, el video se pausa (queda oculto por el CSS).
    escritorio.addEventListener('change', function (e) {
        if (!e.matches && !video.paused) video.pause();
        else if (e.matches && video.dataset.cargado && video.paused) video.play().catch(function () {});
    });
}
