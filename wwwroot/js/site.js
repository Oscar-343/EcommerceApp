// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Navbar compartida (todas las páginas): cambia de estilo al hacer scroll
// y controla el menú móvil. Antes estaba copiado en cada vista.
document.addEventListener('DOMContentLoaded', function () {
    initNavbarScroll();
    initMobileMenu();
    initNavSearch();
    initNavUserMenu();
});

// Buscador de la navbar: el ícono despliega el input; Escape o clic afuera lo cierra.
function initNavSearch() {
    const search = document.getElementById('navSearch');
    const toggle = document.getElementById('navSearchToggle');
    if (!search || !toggle) return;

    toggle.addEventListener('click', function (e) {
        e.stopPropagation();
        const isActive = search.classList.toggle('active');
        toggle.setAttribute('aria-expanded', isActive);
        if (isActive) {
            search.querySelector('.nav-search__input')?.focus();
        }
    });

    document.addEventListener('click', function (e) {
        if (!search.contains(e.target)) {
            search.classList.remove('active');
            toggle.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            search.classList.remove('active');
            toggle.setAttribute('aria-expanded', 'false');
        }
    });
}

// Menú desplegable de usuario en la navbar (Mis pedidos, Mis reservas, Cerrar sesión...).
function initNavUserMenu() {
    const menu = document.getElementById('navUserMenu');
    const toggle = document.getElementById('navUserToggle');
    if (!menu || !toggle) return;

    toggle.addEventListener('click', function (e) {
        e.stopPropagation();
        const isActive = menu.classList.toggle('active');
        toggle.setAttribute('aria-expanded', isActive);
    });

    document.addEventListener('click', function (e) {
        if (!menu.contains(e.target)) {
            menu.classList.remove('active');
            toggle.setAttribute('aria-expanded', 'false');
        }
    });
}

function initNavbarScroll() {
    const navbar = document.getElementById('mainNav');
    if (!navbar) return;

    window.addEventListener('scroll', () => {
        navbar.classList.toggle('scrolled', window.pageYOffset > 50);
    }, { passive: true });
}

// Menú móvil (hamburguesa). Usa una clase (is-open), nunca estilos en línea:
// así, al pasar a escritorio el CSS vuelve a mandar y el menú no queda atascado.
function initMobileMenu() {
    const toggle = document.getElementById('navToggle');
    const menu = document.getElementById('navMenu');
    if (!toggle || !menu) return;

    const icono = toggle.querySelector('i');
    const escritorio = window.matchMedia('(min-width: 992px)');

    function mostrar(abierto) {
        menu.classList.toggle('is-open', abierto);
        document.body.classList.toggle('nav-open', abierto); // bloquea el scroll de la página detrás
        toggle.setAttribute('aria-expanded', String(abierto));
        toggle.setAttribute('aria-label', abierto ? 'Cerrar menú' : 'Abrir menú');
        if (icono) {
            icono.classList.toggle('fa-bars', !abierto);
            icono.classList.toggle('fa-xmark', abierto);
        }
    }

    toggle.addEventListener('click', function (e) {
        e.stopPropagation();
        mostrar(!menu.classList.contains('is-open'));
    });

    // Se cierra al elegir una opción, con Escape o con un clic fuera del menú.
    menu.querySelectorAll('a').forEach(link => link.addEventListener('click', () => mostrar(false)));

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') mostrar(false);
    });

    document.addEventListener('click', function (e) {
        if (menu.classList.contains('is-open') && !menu.contains(e.target) && !toggle.contains(e.target)) {
            mostrar(false);
        }
    });

    // Al pasar a escritorio se limpia el estado móvil.
    escritorio.addEventListener('change', function (e) {
        if (e.matches) mostrar(false);
    });
}
