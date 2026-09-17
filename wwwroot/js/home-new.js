/**
 * HOME - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

document.addEventListener('DOMContentLoaded', function () {
    initNavbarScroll();
    initSmoothScroll();
    initAnimations();
    initMapMarkers();
    initMobileMenu();
    initHeroVideo();
});

/**
 * Cambiar estilo navbar al hacer scroll
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
 * Menú móvil toggle
 */
function initMobileMenu() {
    const toggle = document.getElementById('navToggle');
    const menu = document.querySelector('.nav-menu');
    
    if (!toggle || !menu) return;
    
    toggle.addEventListener('click', function() {
        menu.style.display = menu.style.display === 'flex' ? 'none' : 'flex';
    });
    
    // Cerrar menú al hacer clic en un link
    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', function() {
            menu.style.display = 'none';
        });
    });
}

/**
 * Placeholder para acciones navbar
 */
document.querySelectorAll('.nav-action-icon').forEach(icon => {
    icon.addEventListener('click', function(e) {
        const href = this.getAttribute('href');
        if (href === '#search' || href === '#favorites' || href === '#profile') {
            e.preventDefault();
            console.log('Acción:', href);
            // TODO: Implementar modales
        }
    });
});

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