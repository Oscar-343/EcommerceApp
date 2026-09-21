/* =========================================================
   SERVICIOS / RUTAS — Detalle de Ruta — JavaScript
   Animaciones e interactividad
   ========================================================= */

// El scroll y el menú móvil de la navbar ahora viven en site.js,
// compartidos por todas las páginas.
document.addEventListener('DOMContentLoaded', function() {
    initScrollAnimations();
    initGalleryInteractions();
    initSmoothScroll();
});

/* === ANIMACIONES CON INTERSECTION OBSERVER === */
function initScrollAnimations() {
    const observedElements = document.querySelectorAll(
        '.ruta-info-card, ' +
        '.ruta-section, ' +
        '.ruta-recommendation-item, ' +
        '.ruta-product-card'
    );

    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.animation = 'fadeInUp 0.6s ease-in-out forwards';
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    observedElements.forEach(el => {
        el.style.opacity = '0';
        observer.observe(el);
    });

    // Inyectar animación si no existe
    if (!document.querySelector('style[data-animation="fadeInUp"]')) {
        const style = document.createElement('style');
        style.setAttribute('data-animation', 'fadeInUp');
        style.textContent = `
            @keyframes fadeInUp {
                from {
                    opacity: 0;
                    transform: translateY(20px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
        `;
        document.head.appendChild(style);
    }
}

/* === INTERACCIÓN CON GALERÍA === */
function initGalleryInteractions() {
    const galleryItems = document.querySelectorAll('.ruta-gallery__item img');
    
    galleryItems.forEach(item => {
        item.addEventListener('click', function() {
            // Preparado para expandir imagen en lightbox futuro
            console.log('Galería item clicked:', this.src);
        });
    });
}

/* === SMOOTH SCROLL === */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

/* === BOTÓN VOLVER === */
document.addEventListener('DOMContentLoaded', function() {
    const backButton = document.querySelector('.ruta-hero__back');
    if (backButton && !backButton.href) {
        backButton.addEventListener('click', function(e) {
            e.preventDefault();
            window.history.back();
        });
    }
});
