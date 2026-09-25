// Panel admin: barra lateral en móvil y tablet (menos de 992px).
// Misma técnica que el menú público (site.js): clases is-open y
// aria-expanded, nunca estilos en línea. Desde 992px el CSS la muestra fija.
document.addEventListener('DOMContentLoaded', function () {
    initAdminSidebar();
});

function initAdminSidebar() {
    const toggle = document.getElementById('adminSidebarToggle');
    const sidebar = document.getElementById('adminSidebar');
    const overlay = document.getElementById('adminSidebarOverlay');
    if (!toggle || !sidebar || !overlay) return;

    const escritorio = window.matchMedia('(min-width: 992px)');

    function mostrar(abierto) {
        sidebar.classList.toggle('is-open', abierto);
        overlay.classList.toggle('is-open', abierto);
        document.body.classList.toggle('admin-nav-open', abierto); // bloquea el scroll de atrás
        toggle.setAttribute('aria-expanded', String(abierto));
        toggle.setAttribute('aria-label', abierto ? 'Cerrar menú' : 'Abrir menú');
    }

    toggle.addEventListener('click', function () {
        mostrar(!sidebar.classList.contains('is-open'));
    });

    // Se cierra con un clic en el fondo, al elegir una opción o con Escape.
    overlay.addEventListener('click', () => mostrar(false));
    sidebar.querySelectorAll('a').forEach(link => link.addEventListener('click', () => mostrar(false)));

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar.classList.contains('is-open')) mostrar(false);
    });

    // Al pasar a escritorio se limpia el estado móvil.
    escritorio.addEventListener('change', function (e) {
        if (e.matches) mostrar(false);
    });
}
