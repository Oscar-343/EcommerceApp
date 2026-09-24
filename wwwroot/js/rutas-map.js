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

    // Quita el enlace "Leaflet" (opcional). El crédito del mapa sí es obligatorio.
    map.attributionControl.setPrefix(false);

    // Mapa satelital de Esri (gratis, sin API key; exige mostrar el crédito).
    L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Powered by Esri | Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community',
        maxZoom: 18
    }).addTo(map);

    const markerIcon = L.divIcon({
        className: 'rutas-map-marker',
        html: '<span></span>',
        iconSize: [16, 16]
    });

    const bounds = [];
    routes.forEach(function (route) {
        const marker = L.marker([route.lat, route.lng], { icon: markerIcon }).addTo(map);
        marker.bindPopup(crearPopup(route));
        bounds.push([route.lat, route.lng]);
    });

    // Límites aproximados de Bolivia (suroeste, noreste).
    // Se usan límites y no un zoom fijo para que se adapte al tamaño de pantalla.
    const BOLIVIA_BOUNDS = [[-22.9, -69.7], [-9.6, -57.4]];

    if (el.dataset.initialView === 'bolivia') {
        map.fitBounds(BOLIVIA_BOUNDS, { padding: [20, 20] });
    } else if (bounds.length === 1) {
        map.setView(bounds[0], 8);
    } else {
        map.fitBounds(bounds, { padding: [40, 40] });
    }
}

// Arma el popup con textContent para que ningún texto se interprete como HTML.
function crearPopup(route) {
    const box = document.createElement('div');
    box.className = 'rutas-map-popup';

    const titulo = document.createElement('strong');
    titulo.textContent = route.name;
    box.appendChild(titulo);

    // Línea de detalle: ubicación · dificultad · duración (solo las que existan)
    const detalles = [route.location, route.difficulty, route.duration ? route.duration + ' h' : null]
        .filter(Boolean);
    if (detalles.length) {
        const p = document.createElement('div');
        p.textContent = detalles.join(' · ');
        box.appendChild(p);
    }

    if (route.id) {
        const link = document.createElement('a');
        link.href = '/Services/Details/' + encodeURIComponent(route.id);
        link.textContent = 'Ver ruta →';
        box.appendChild(link);
    }

    return box;
}
