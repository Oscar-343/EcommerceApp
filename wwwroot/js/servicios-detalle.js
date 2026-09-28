/**
 * DETALLE DE RUTA (Views/Services/Details.cshtml)
 * - Galería: la miniatura elegida pasa a la foto grande.
 * - Reserva: botones − / + de personas y total estimado (el servidor lo vuelve a calcular).
 * - Corazón de favoritos sin recargar la página.
 * - Aparición suave de los paneles al hacer scroll.
 */
document.addEventListener('DOMContentLoaded', function () {
    initGaleria();
    initReserva();
    initFavorito();
    initAparicion();
});

function initGaleria() {
    const foto = document.getElementById('rdetFoto');
    if (!foto) return;

    document.querySelectorAll('.rdet-galeria__thumb').forEach(function (thumb) {
        thumb.addEventListener('click', function () {
            foto.src = thumb.dataset.foto;
            document.querySelectorAll('.rdet-galeria__thumb').forEach(function (t) { t.classList.remove('active'); });
            thumb.classList.add('active');
        });
    });
}

function initReserva() {
    const input = document.getElementById('peopleCount');
    const total = document.getElementById('totalEstimado');
    if (!input || !total) return;

    const precio = parseFloat(input.dataset.precio) || 0;
    const max = parseInt(input.max, 10) || 50;

    function actualizar() {
        let n = parseInt(input.value, 10) || 1;
        n = Math.min(Math.max(n, 1), max);
        input.value = n;
        total.textContent = 'Bs. ' + (precio * n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    document.querySelectorAll('[data-personas]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            input.value = (parseInt(input.value, 10) || 1) + parseInt(btn.dataset.personas, 10);
            actualizar();
        });
    });
    input.addEventListener('change', actualizar);
}

function initFavorito() {
    const form = document.querySelector('[data-fav-form]');
    if (!form) return;

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const boton = form.querySelector('button');
        boton.disabled = true;

        try {
            const resp = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            // Sin sesión iniciada, el servidor redirige al login.
            if (resp.redirected || resp.status === 401) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname);
                return;
            }

            const data = await resp.json();
            if (data.success) {
                const activo = data.isFavorite;
                boton.classList.toggle('is-active', activo);
                boton.setAttribute('aria-pressed', activo ? 'true' : 'false');
                boton.setAttribute('aria-label', activo ? 'Quitar de favoritos' : 'Guardar en favoritos');
                const icono = boton.querySelector('i');
                icono.classList.toggle('fa-solid', activo);
                icono.classList.toggle('fa-regular', !activo);
            }
        } catch (err) {
            form.submit();
        } finally {
            boton.disabled = false;
        }
    });
}

function initAparicion() {
    if (!('IntersectionObserver' in window)) return;
    const bloques = document.querySelectorAll('.cuenta-main > *');

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.remove('rdet-oculto');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.08 });

    bloques.forEach(function (b, i) {
        if (i === 0) return; // el primero ya está a la vista
        b.classList.add('rdet-oculto');
        observer.observe(b);
    });
}
