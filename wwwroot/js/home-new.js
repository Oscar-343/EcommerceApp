/**
 * HOME - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

// El scroll y el menú móvil de la navbar ahora viven en site.js
// (initNavbarScroll / initMobileMenu), compartidos por todas las páginas.
document.addEventListener('DOMContentLoaded', function () {
    initSmoothScroll();
    initAnimations();
    initMapMarkers();
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
 * Animaciones de entrada con IntersectionObserver
 */
function initAnimations() {
    if (!('IntersectionObserver' in window)) {
        // Fallback para navegadores antiguos
        document.querySelectorAll('.home-animate').forEach(el => {
            el.style.opacity = '1';
        });
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

    document.querySelectorAll('.home-animate').forEach(el => {
        observer.observe(el);
    });
}

/**
 * Interactividad de marcadores del mapa
 */
function initMapMarkers() {
    const markers = document.querySelectorAll('.map-marker');
    
    markers.forEach(marker => {
        marker.addEventListener('click', function() {
            const routeId = this.getAttribute('data-route-id');
            console.log('Ruta seleccionada:', routeId);
            // TODO: Navegar al detalle de la ruta
        });
        
        marker.addEventListener('mouseenter', function() {
            const tooltip = this.querySelector('.marker-tooltip');
            if (tooltip) {
                tooltip.style.opacity = '1';
                tooltip.style.visibility = 'visible';
            }
        });
        
        marker.addEventListener('mouseleave', function() {
            const tooltip = this.querySelector('.marker-tooltip');
            if (tooltip) {
                tooltip.style.opacity = '0';
                tooltip.style.visibility = 'hidden';
            }
        });
    });
}

/**
 * Inicializar y forzar reproducción del video hero
 */
function initHeroVideo() {
    const video = document.querySelector('.hero-video');

    if (!video) {
        console.error('No se encontró el elemento .hero-video');
        return;
    }

    // Configuración para autoplay
    video.muted = true;
    video.volume = 0;
    video.autoplay = true;
    video.loop = true;
    video.playsInline = true;

    // Información de carga
    video.addEventListener('loadedmetadata', function () {
        console.log('VIDEO: metadata cargada');
        console.log('Duración:', video.duration);
        console.log('Resolución:', video.videoWidth + 'x' + video.videoHeight);
    });

    video.addEventListener('loadeddata', function () {
        console.log('VIDEO: datos cargados');
    });

    video.addEventListener('canplay', function () {
        console.log('VIDEO: listo para reproducirse');
    });

    video.addEventListener('playing', function () {
        console.log('VIDEO: REPRODUCIÉNDOSE');
    });

    video.addEventListener('pause', function () {
        console.log('VIDEO: pausado');
    });

    video.addEventListener('error', function () {
        console.error('VIDEO: ERROR');
        console.error(video.error);
    });

    // Intentar reproducir
    video.play()
        .then(function () {
            console.log('VIDEO: autoplay iniciado correctamente');
        })
        .catch(function (error) {
            console.error('VIDEO: no se pudo iniciar autoplay');
            console.error(error);
        });
}