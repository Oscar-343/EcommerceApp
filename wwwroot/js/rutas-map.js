/**
 * Mapa de rutas con Leaflet + tiles oscuros (CartoDB Dark), sin API key.
 * Compartido por Home, Rutas (listado) y Detalle de ruta.
 * Lee las coordenadas desde el atributo data-routes del contenedor #rutasMap.
 */
document.addEventListener('DOMContentLoaded', function () {
    initRutasMap();
});

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
            (route.id ? '<br><a href="/Services/Details/' + route.id + '">Ver ruta →</a>' : '') +
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
