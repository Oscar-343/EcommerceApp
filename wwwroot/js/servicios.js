/**
 * SERVICIOS - CINEMATOGRÁFICO OUTDOOR PREMIUM
 * JavaScript para interactividad y efectos suaves
 */

// El scroll y el menú móvil de la navbar ahora viven en site.js,
// compartidos por todas las páginas.
document.addEventListener('DOMContentLoaded', function () {
    initAnimations();
    initRutasHero();
    initRutasMap();
});

/**
 * Hero rotativo de rutas: cambia automáticamente (7s) o al hacer clic en
 * una de las tarjetas laterales / flechas. Pausable con el botón play/pausa.
 */
function initRutasHero() {
    const hero = document.getElementById('rutasHero');
    if (!hero) return;

    const slides = hero.querySelectorAll('.rutas-hero__slide[data-index]');
    const infos = hero.querySelectorAll('.rutas-hero__info[data-index]');
    const stackItems = hero.querySelectorAll('.rutas-hero__stack-item[data-index]');
    const counter = document.getElementById('rutasHeroCounter');
    const total = slides.length;
    if (total <= 1) return;

    let current = 0;
    let playing = true;
    let timer = null;

    function goTo(index) {
        current = ((index % total) + total) % total;

        slides.forEach(el => el.classList.toggle('active', +el.dataset.index === current));
        infos.forEach(el => el.classList.toggle('active', +el.dataset.index === current));
        stackItems.forEach(el => el.classList.toggle('active', +el.dataset.index === current));
        if (counter) counter.textContent = String(current + 1).padStart(2, '0');
    }

    function next() { goTo(current + 1); }
    function prev() { goTo(current - 1); }

    function startAutoplay() {
        stopAutoplay();
        timer = setInterval(next, 7000);
    }
    function stopAutoplay() {
        if (timer) clearInterval(timer);
        timer = null;
    }

    document.getElementById('rutasHeroNext')?.addEventListener('click', function () { next(); if (playing) startAutoplay(); });
    document.getElementById('rutasHeroPrev')?.addEventListener('click', function () { prev(); if (playing) startAutoplay(); });

    stackItems.forEach(item => {
        item.addEventListener('click', function () {
            goTo(+this.dataset.index);
            if (playing) startAutoplay();
        });
    });

    const playBtn = document.getElementById('rutasHeroPlay');
    playBtn?.addEventListener('click', function () {
        playing = !playing;
        this.innerHTML = playing ? '<i class="fas fa-pause"></i>' : '<i class="fas fa-play"></i>';
        if (playing) startAutoplay(); else stopAutoplay();
    });

    startAutoplay();
}

/**
 * Mapa de rutas con Leaflet + tiles oscuros (CartoDB Dark), sin API key.
 * Lee las coordenadas desde el atributo data-routes del contenedor.
 */
function initRutasMap() {
    const el = document.getElementById('rutasMap');
    if (!el || typeof L === 'undefined') return;

    let routes = [];
    try {
        routes = JSON.parse(el.dataset.routes || '[]');
    } catch (e) {
        return;
    }
    if (!routes.length) return;

    const map = L.map(el, { scrollWheelZoom: false });

    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 19
    }).addTo(map);

    const markerIcon = L.divIcon({
        className: 'rutas-map-marker',
        html: '<span></span>',
        iconSize: [16, 16]
    });

    const bounds = [];
    routes.forEach(function (route) {
        const marker = L.marker([route.lat, route.lng], { icon: markerIcon }).addTo(map);
        marker.bindPopup(
            '<div class="rutas-map-popup">' +
            '<strong>' + route.name + '</strong>' +
            (route.location ? '<br>' + route.location : '') +
            '<br><a href="/Services/Details/' + route.id + '">Ver ruta →</a>' +
            '</div>'
        );
        bounds.push([route.lat, route.lng]);
    });

    if (bounds.length === 1) {
        map.setView(bounds[0], 8);
    } else {
        map.fitBounds(bounds, { padding: [40, 40] });
    }
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
