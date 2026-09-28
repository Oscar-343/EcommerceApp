/**
 * RUTAS (Views/Services/Index.cshtml)
 * - Hero rotativo: cambia de ruta cada 7 s o al tocar una tarjeta lateral.
 * - Los <select> de la barra filtran al cambiar (sin botón).
 * - El corazón de cada tarjeta guarda/quita favoritos sin recargar la página.
 * - Las tarjetas aparecen con un desplazamiento suave al entrar en pantalla.
 */
document.addEventListener('DOMContentLoaded', function () {
    initRutasHero();
    initFiltros();
    initFavoritos();
    initAparicion();
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
        this.innerHTML = playing ? '<i class="fa-solid fa-pause"></i>' : '<i class="fa-solid fa-play"></i>';
        if (playing) startAutoplay(); else stopAutoplay();
    });

    startAutoplay();
}

function initFiltros() {
    const form = document.getElementById('rutasFiltrosForm');
    if (!form) return;

    const min = document.getElementById('minDuration');
    const max = document.getElementById('maxDuration');

    form.querySelectorAll('select').forEach(function (select) {
        select.addEventListener('change', function () {
            // La duración viaja como "min-max" en un solo select; se reparte en los dos campos ocultos.
            const duracion = form.querySelector('[data-duracion]');
            if (duracion) {
                const [desde, hasta] = duracion.value.split('-');
                min.value = duracion.value ? (desde || '') : '';
                max.value = duracion.value ? (hasta || '') : '';
            }

            // Los campos vacíos no se envían, para que la URL quede limpia.
            form.querySelectorAll('input, select').forEach(function (campo) {
                campo.disabled = !campo.value || campo.hasAttribute('data-duracion');
            });
            form.submit();
        });
    });
}

function initFavoritos() {
    document.querySelectorAll('[data-fav-form]').forEach(function (form) {
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

                // Sin sesión iniciada, el servidor redirige al login: se sigue esa ruta.
                if (resp.redirected || resp.status === 401) {
                    window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
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
                form.submit(); // si algo falla, se hace el envío normal
            } finally {
                boton.disabled = false;
            }
        });
    });
}

function initAparicion() {
    const tarjetas = document.querySelectorAll('.ruta-card');
    if (!('IntersectionObserver' in window)) return;

    tarjetas.forEach(function (t) { t.classList.add('is-oculta'); });

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.remove('is-oculta');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    tarjetas.forEach(function (t) { observer.observe(t); });
}
