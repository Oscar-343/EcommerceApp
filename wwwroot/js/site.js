// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Navbar compartida (todas las páginas): cambia de estilo al hacer scroll
// y controla el menú móvil. Antes estaba copiado en cada vista.
document.addEventListener('DOMContentLoaded', function () {
    initNavbarScroll();
    initMobileMenu();
});

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
