/**
 * Formulario de rutas (admin): al pegar un enlace de Google Maps,
 * pide las coordenadas al servidor (Admin/ResolveMapLink) y llena
 * los campos de latitud y longitud.
 */
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-map-link]').forEach(iniciarCampo);
    });

    function iniciarCampo(input) {
        var estado = document.getElementById(input.dataset.statusId);
        var campoLat = document.getElementById(input.dataset.latTarget);
        var campoLng = document.getElementById(input.dataset.lngTarget);
        var urlServidor = input.dataset.resolveUrl;
        var ultimoEnlace = '';

        // En "Editar": si la ruta ya tiene coordenadas, se muestran para verificarlas.
        if (campoLat.value && campoLng.value) {
            mostrarUbicacion(estado, 'Ubicación actual:', campoLat.value, campoLng.value);
        }

        function resolver() {
            var enlace = input.value.trim();
            if (!enlace || enlace === ultimoEnlace) return; // evita consultar dos veces lo mismo
            ultimoEnlace = enlace;
            mostrarMensaje(estado, 'Buscando coordenadas…', '');

            var datos = new FormData();
            datos.append('url', enlace);
            datos.append('__RequestVerificationToken', obtenerToken(input));

            fetch(urlServidor, { method: 'POST', body: datos })
                .then(function (respuesta) { return respuesta.json(); })
                .then(function (resultado) {
                    if (!resultado.ok) {
                        mostrarMensaje(estado, resultado.mensaje, 'error');
                        return;
                    }
                    campoLat.value = resultado.lat;
                    campoLng.value = resultado.lng;
                    mostrarUbicacion(estado, '✓', resultado.lat, resultado.lng);
                })
                .catch(function () {
                    ultimoEnlace = ''; // permite reintentar
                    mostrarMensaje(estado, 'No se pudo consultar el enlace. Ingresa las coordenadas a mano.', 'error');
                });
        }

        // "paste" ocurre antes de que el texto llegue al campo: se espera un instante.
        input.addEventListener('paste', function () { setTimeout(resolver, 0); });
        input.addEventListener('change', resolver);

        // Enter en este campo no debe enviar el formulario completo.
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); resolver(); }
        });
    }

    // Texto simple de estado (textContent: nada se interpreta como HTML).
    function mostrarMensaje(estado, texto, tipo) {
        estado.className = 'map-link-status' + (tipo ? ' map-link-status--' + tipo : '');
        estado.textContent = texto;
    }

    // "✓ -17.35, -65.8667  Ver en el mapa" con enlace para comprobar el punto.
    function mostrarUbicacion(estado, prefijo, lat, lng) {
        mostrarMensaje(estado, prefijo + ' ' + lat + ', ' + lng, 'ok');
        var link = document.createElement('a');
        link.href = 'https://www.google.com/maps?q=' + encodeURIComponent(lat + ',' + lng);
        link.target = '_blank';
        link.rel = 'noopener';
        link.textContent = 'Ver en el mapa';
        estado.appendChild(link);
    }

    function obtenerToken(input) {
        var campo = input.form && input.form.querySelector('input[name="__RequestVerificationToken"]');
        return campo ? campo.value : '';
    }
})();
