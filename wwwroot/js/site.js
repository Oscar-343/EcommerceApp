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

function initMobileMenu() {
    const toggle = document.getElementById('navToggle');
    const menu = document.querySelector('.nav-menu');
    if (!toggle || !menu) return;

    toggle.addEventListener('click', function () {
        menu.style.display = menu.style.display === 'flex' ? 'none' : 'flex';
    });

    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', function () {
            menu.style.display = 'none';
        });
    });
}
