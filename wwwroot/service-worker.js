/* =========================================================
   TREN AL SUR — Service Worker
   Vive en la raíz de wwwroot para que su scope sea "/" (todo el sitio).
   Subir CACHE_VERSION borra todas las cachés "tas-*" anteriores al activarse.
   ========================================================= */
const CACHE_VERSION = 'v1';
const STATIC_CACHE  = `tas-static-${CACHE_VERSION}`;
const PAGES_CACHE   = `tas-pages-${CACHE_VERSION}`;
const IMAGES_CACHE  = `tas-images-${CACHE_VERSION}`;
const OFFLINE_URL   = '/offline.html';

const PAGES_MAX_ENTRIES  = 40;
const IMAGES_MAX_ENTRIES = 60;

// Rutas que nunca pasan por la caché (network-only). Son los controladores reales:
// CartController, OrdersController (incluye Checkout), ReservationsController,
// FavoritesController, AccountController, AdminController, la API y los callbacks
// del login social de Google/GitHub. Se comparan en minúsculas y por segmento.
const SENSITIVE_PREFIXES = [
    '/cart',
    '/orders',
    '/reservations',
    '/favorites',
    '/account',
    '/admin',
    '/api',
    '/signin-google',
    '/signin-github'
];

// Navegaciones que se guardan para verlas sin conexión (network-first):
// Home, Productos (lista y detalle) y Rutas = ServicesController (lista y detalle).
const CACHEABLE_PAGE_PREFIXES = ['/home', '/products', '/services'];

// Recursos base que se precargan al instalar (mismas rutas que usa _Layout.cshtml).
const PRECACHE_URLS = [
    OFFLINE_URL,
    '/manifest.webmanifest',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/icons/icon-maskable-512.png',
    '/icons/apple-touch-icon.png',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/css/tokens.css',
    '/css/site.css',
    '/css/pwa-install.css',
    '/js/site.js',
    '/js/pwa.js'
];

/* ---------- Ciclo de vida ---------- */

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
            // cache: 'reload' evita precargar copias viejas de la caché HTTP del navegador.
            .then((cache) => cache.addAll(PRECACHE_URLS.map((url) => new Request(url, { cache: 'reload' }))))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    const current = [STATIC_CACHE, PAGES_CACHE, IMAGES_CACHE];
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(
                keys
                    .filter((key) => key.startsWith('tas-') && !current.includes(key))
                    .map((key) => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

/* ---------- Enrutado de peticiones (la primera regla que coincide gana) ---------- */

self.addEventListener('fetch', (event) => {
    const request = event.request;

    // 1. Solo GET. POST de carrito, login, checkout, etc. van directo a la red.
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    const sameOrigin = url.origin === self.location.origin;
    const path = url.pathname.toLowerCase();

    // 2. Rutas sensibles: network-only, nunca se guarda nada.
    //    Si es una navegación y no hay red, se muestra la página offline genérica.
    if (sameOrigin && matchesPrefix(path, SENSITIVE_PREFIXES)) {
        if (request.mode === 'navigate') {
            event.respondWith(fetch(request).catch(() => caches.match(OFFLINE_URL)));
        }
        return;
    }

    // 3. Otros orígenes (CDN de íconos, Google Fonts, mapas, etc.): no se interceptan,
    //    salvo las imágenes públicas de Supabase Storage.
    const supabaseImage = isSupabaseImage(url, request);
    if (!sameOrigin && !supabaseImage) return;

    // 4. Estáticos propios: cache-first.
    if (sameOrigin && isStaticAsset(path)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // 5. Imágenes (Supabase y locales): stale-while-revalidate.
    if (supabaseImage || (sameOrigin && request.destination === 'image')) {
        event.respondWith(staleWhileRevalidate(event, request, supabaseImage));
        return;
    }

    if (request.mode === 'navigate') {
        // 6. Home, Productos y Rutas: network-first con copia para offline.
        if (isCacheablePage(path)) {
            event.respondWith(networkFirstPage(request));
            return;
        }

        // 7. Resto de navegaciones: red, y sin red la página offline.
        event.respondWith(fetch(request).catch(() => caches.match(OFFLINE_URL)));
    }
    // Cualquier otra petición sigue su curso normal sin pasar por el SW.
});

/* ---------- Clasificación ---------- */

// Coincide por segmento: "/cart" cubre "/cart" y "/cart/add", pero no "/cartografia".
function matchesPrefix(path, prefixes) {
    return prefixes.some((prefix) => path === prefix || path.startsWith(prefix + '/'));
}

function isStaticAsset(path) {
    return /\.(css|js|woff2?)$/.test(path) || path.startsWith('/icons/') || path.startsWith('/lib/');
}

// Solo imágenes (no videos) del bucket público de Supabase Storage.
function isSupabaseImage(url, request) {
    return url.hostname.endsWith('.supabase.co') &&
        url.pathname.startsWith('/storage/v1/object/public/') &&
        request.destination === 'image';
}

function isCacheablePage(path) {
    return path === '/' || matchesPrefix(path, CACHEABLE_PAGE_PREFIXES);
}

// Solo respuestas correctas y legibles. Las "opaque" (sin CORS) se aceptan
// únicamente para imágenes de Supabase, porque no se puede ver si son un error.
function isCacheable(response, allowOpaque = false) {
    if (!response) return false;
    if (allowOpaque && response.type === 'opaque') return true;
    return response.ok && (response.type === 'basic' || response.type === 'cors');
}

/* ---------- Estrategias ---------- */

async function cacheFirst(request) {
    const cache = await caches.open(STATIC_CACHE);
    const cached = await cache.match(request);
    if (cached) return cached;

    try {
        const response = await fetch(request);
        if (isCacheable(response)) {
            // asp-append-version cambia el ?v= en cada deploy: se borra la copia
            // anterior del mismo archivo para que la caché no crezca sin límite.
            await cache.delete(request, { ignoreSearch: true });
            await cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        // Sin red: cualquier versión guardada del archivo sirve (p. ej. la precargada sin ?v=).
        const fallback = await cache.match(request, { ignoreSearch: true });
        if (fallback) return fallback;
        throw error;
    }
}

async function staleWhileRevalidate(event, request, fromSupabase) {
    const cache = await caches.open(IMAGES_CACHE);
    const cached = await cache.match(request);

    // Supabase Storage responde con CORS: se pide en modo "cors" para obtener una
    // respuesta legible (las opaque ocupan mucha cuota). Si falla, se usa la original.
    const networkFetch = fromSupabase
        ? fetch(request.url, { mode: 'cors', credentials: 'omit' }).catch(() => fetch(request))
        : fetch(request);

    const update = networkFetch.then(async (response) => {
        if (isCacheable(response, fromSupabase)) {
            await cache.put(request, response.clone());
            await trimCache(IMAGES_CACHE, IMAGES_MAX_ENTRIES);
        }
        return response;
    });

    if (cached) {
        event.waitUntil(update.catch(() => {})); // se actualiza en segundo plano
        return cached;
    }
    return update;
}

async function networkFirstPage(request) {
    const cacheKey = pageCacheKey(request.url);

    try {
        const response = await fetch(request);
        // El navbar muestra el nombre del usuario y los contadores de carrito/favoritos.
        // Program.cs marca con "X-TAS-Page-Cache: anon" solo el HTML de visitantes
        // anónimos; si falta el header, la página puede tener datos personales y no se guarda.
        if (isCacheable(response) && response.headers.get('X-TAS-Page-Cache') === 'anon') {
            const cache = await caches.open(PAGES_CACHE);
            await cache.put(cacheKey, response.clone());
            await trimCache(PAGES_CACHE, PAGES_MAX_ENTRIES);
        }
        return response;
    } catch (error) {
        const cached = await caches.match(cacheKey, { cacheName: PAGES_CACHE });
        return cached || caches.match(OFFLINE_URL);
    }
}

// "/?source=pwa" (start_url del manifest) y "/" deben compartir la misma copia.
function pageCacheKey(href) {
    const url = new URL(href);
    url.searchParams.delete('source');
    return url.href;
}

// Borra las entradas más antiguas (Cache Storage conserva el orden de inserción).
async function trimCache(cacheName, maxEntries) {
    const cache = await caches.open(cacheName);
    const keys = await cache.keys();
    for (let i = 0; i < keys.length - maxEntries; i++) {
        await cache.delete(keys[i]);
    }
}
