/**
 * SERVICIOS / RUTAS - Sistema de Experiencias de Senderismo
 * Funcionalidad de cliente para filtros, animaciones e interacciones
 */

document.addEventListener('DOMContentLoaded', function () {
    // === INICIALIZAR COMPONENTES ===
    initAnimations();
    initScrollEffects();
});

/**
 * Inicializar animaciones de entrada
 */
function initAnimations() {
    const animatedElements = document.querySelectorAll('.servicios-animate');

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
 * Efectos de scroll (parallax sutil en el hero)
 */
function initScrollEffects() {
    const hero = document.querySelector('.servicios-hero');
    
    if (!hero) return;

    window.addEventListener('scroll', () => {
        const scrolled = window.pageYOffset;
        const heroHeight = hero.offsetHeight;
        
        if (scrolled < heroHeight) {
            // Parallax muy sutil
            hero.style.transform = `translateY(${scrolled * 0.3}px)`;
        }
    }, { passive: true });
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
 * Reservar ruta (placeholder)
 */
function reservarRuta(routeId) {
    // TODO: Implementar modal de reserva con selección de:
    // - Fecha
    // - Cantidad de personas
    // - Datos del reservante
    console.log('Reservando ruta:', routeId);
    
    // Placeholder temporal
    alert('Funcionalidad de reserva en desarrollo.\n\nPróximamente podrás:\n• Seleccionar fecha\n• Elegir cantidad de personas\n• Confirmar la reserva');
}

/**
 * Abrir galería de imágenes (placeholder)
 */
function openGallery(index) {
    console.log('Abriendo galería en índice:', index);
    // TODO: Implementar visor de galería modal
}

/**
 * Filtrar rutas (helper para formularios)
 */
function applyFilters() {
    const form = document.querySelector('.servicios-filters form');
    if (form) {
        form.submit();
    }
}

/**
 * Limpiar filtros
 */
function clearFilters() {
    window.location.href = window.location.pathname;
}

/**
 * Exportar funciones para uso global
 */
window.serviciosModule = {
    reservarRuta,
    openGallery,
    applyFilters,
    clearFilters
};
