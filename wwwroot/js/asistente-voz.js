/**
 * Asistente de voz de Tren al Sur.
 * Escucha lo que dice el cliente (Web Speech API, sin backend ni costo),
 * interpreta la intención con palabras clave y lleva a la página que corresponde.
 *
 * Soporte: Chrome, Edge y Brave. En Firefox y la mayoría de Safari no existe
 * SpeechRecognition: en ese caso el botón no se muestra.
 */
(function () {

    // ---------------------------------------------------------------
    // DICCIONARIOS (texto sin tildes y en minúscula)
    // ---------------------------------------------------------------

    // Palabra dicha -> categoría exacta de la tienda.
    // Los valores deben coincidir con ProductCategories.All (Models/ProductCategories.cs).
    var CATEGORIAS_PRODUCTO = {
        'bota': 'Calzado de trekking', 'botas': 'Calzado de trekking',
        'zapatilla': 'Calzado de trekking', 'zapatillas': 'Calzado de trekking',
        'calzado': 'Calzado de trekking', 'zapatos': 'Calzado de trekking',
        'mochila': 'Mochilas', 'mochilas': 'Mochilas',
        'carpa': 'Tiendas de campaña', 'carpas': 'Tiendas de campaña',
        'baston': 'Bastones de trekking', 'bastones': 'Bastones de trekking',
        'chaqueta': 'Ropa outdoor', 'chaquetas': 'Ropa outdoor',
        'campera': 'Ropa outdoor', 'camperas': 'Ropa outdoor',
        'pantalon': 'Ropa outdoor', 'pantalones': 'Ropa outdoor',
        'ropa': 'Ropa outdoor',
        'linterna': 'Iluminación', 'linternas': 'Iluminación',
        'frontal': 'Iluminación', 'frontales': 'Iluminación',
        'botella': 'Hidratación', 'botellas': 'Hidratación',
        'termo': 'Hidratación', 'hidratacion': 'Hidratación',
        'brujula': 'Seguridad y orientación', 'gps': 'Seguridad y orientación',
        'botiquin': 'Seguridad y orientación',
        'camping': 'Camping', 'accesorios': 'Accesorios'
    };

    // Palabras que indican que busca productos aunque no diga una categoría.
    var PALABRAS_PRODUCTO = ['producto', 'productos', 'tienda', 'equipo', 'equipamiento', 'comprar'];

    // Palabras que indican que busca rutas.
    var PALABRAS_RUTA = ['ruta', 'rutas', 'trekking', 'senderismo', 'caminata', 'caminatas',
        'excursion', 'excursiones', 'aventura', 'aventuras', 'servicios', 'tour', 'tours'];

    // Dificultad dicha -> valor que usa Service.Difficulty.
    var DIFICULTADES = {
        'facil': 'easy', 'faciles': 'easy', 'sencilla': 'easy', 'sencillas': 'easy', 'principiante': 'easy',
        'moderada': 'moderate', 'moderadas': 'moderate', 'intermedia': 'moderate', 'intermedias': 'moderate',
        'dificil': 'difficult', 'dificiles': 'difficult', 'exigente': 'difficult', 'exigentes': 'difficult',
        'extrema': 'extreme', 'extremas': 'extreme'
    };

    // Para el mensaje que ve el usuario.
    var NOMBRE_DIFICULTAD = { easy: 'fáciles', moderate: 'moderadas', difficult: 'difíciles', extreme: 'extremas' };

    // Región dicha -> valor de RouteRegions.All.
    var REGIONES = { 'cochabamba': 'Cochabamba', 'bolivia': 'Bolivia', 'internacional': 'Internacional', 'extranjero': 'Internacional' };

    // Modificadores de la tienda.
    var PALABRAS_BARATO = ['barato', 'barata', 'baratos', 'baratas', 'economico', 'economica', 'economicos', 'economicas'];
    var PALABRAS_CARO = ['caro', 'cara', 'caros', 'caras', 'premium'];
    var PALABRAS_OFERTA = ['oferta', 'ofertas', 'descuento', 'descuentos', 'promocion', 'promociones', 'rebaja', 'rebajas'];
    var PALABRAS_NUEVO = ['nuevo', 'nueva', 'nuevos', 'nuevas', 'novedades'];

    // Palabras sin significado para la búsqueda ("quiero buscar una ruta a...").
    var PALABRAS_VACIAS = ['quiero', 'quisiera', 'buscar', 'busca', 'busco', 'buscame', 'muestrame', 'mostrar',
        'muestra', 'ver', 'veo', 'dame', 'necesito', 'ir', 'vamos', 'llevame', 'abrir', 'abre', 'hay', 'tienes',
        'tienen', 'algo', 'alguna', 'algun', 'algunas', 'algunos', 'un', 'una', 'unos', 'unas', 'el', 'la', 'los',
        'las', 'a', 'al', 'de', 'del', 'en', 'para', 'por', 'con', 'que', 'y', 'o', 'me', 'mi', 'mis', 'hacia',
        'cerca', 'sobre', 'favor', 'porfa', 'bien', 'muy', 'mas', 'menos', 'bolivianos', 'bs', 'pesos',
        'hasta', 'maximo', 'minimo', 'desde', 'reservar', 'reservo'];

    // ---------------------------------------------------------------
    // INTERPRETAR: texto dicho -> { url, mensaje }
    // No toca la página: se puede probar en la consola con
    // AsistenteVoz.interpretar('botas baratas')
    // ---------------------------------------------------------------

    // Quita tildes y pasa a minúscula, para comparar sin importar cómo se dictó.
    function normalizar(texto) {
        return texto.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    }

    function interpretar(textoOriginal, urls) {
        var texto = normalizar(textoOriginal);

        // Cada palabra guarda su versión normalizada (para comparar)
        // y la original (para buscar en la BD con sus tildes).
        var originales = textoOriginal.toLowerCase().replace(/[¿?¡!.,]/g, ' ').split(/\s+/).filter(Boolean);
        var palabras = originales.map(function (p) {
            return { norm: normalizar(p), orig: p };
        });
        var tiene = function (lista) {
            return palabras.some(function (p) { return lista.indexOf(p.norm) !== -1; });
        };

        // 1. Páginas del usuario (frases directas).
        if (/\bcarrito\b/.test(texto)) return { url: urls.carrito, mensaje: 'Abriendo tu carrito' };
        if (/\bfavoritos?\b/.test(texto)) return { url: urls.favoritos, mensaje: 'Abriendo tus favoritos' };
        if (/\bpedidos?\b/.test(texto)) return { url: urls.pedidos, mensaje: 'Abriendo tus pedidos' };
        // "reservas?" no coincide con "reservar": "quiero reservar Corani" sigue a rutas.
        if (/\breservas?\b/.test(texto)) return { url: urls.reservas, mensaje: 'Abriendo tus reservas' };
        if (/\b(inicio|portada|home)\b/.test(texto)) return { url: urls.inicio, mensaje: 'Volviendo al inicio' };

        // 2. Productos: se revisa antes que rutas porque "botas de senderismo"
        //    incluye una palabra de rutas pero se refiere a un producto.
        var categoria = null;
        palabras.forEach(function (p) {
            if (!categoria && CATEGORIAS_PRODUCTO[p.norm]) categoria = CATEGORIAS_PRODUCTO[p.norm];
        });

        if (categoria || tiene(PALABRAS_PRODUCTO)) {
            var filtrosProducto = {};
            var descripcion = [categoria ? categoria.toLowerCase() : 'productos'];

            if (categoria) filtrosProducto.category = categoria;
            if (tiene(PALABRAS_OFERTA)) { filtrosProducto.onlyOffers = 'true'; descripcion.push('en oferta'); }
            if (tiene(PALABRAS_BARATO)) { filtrosProducto.orderBy = 'price-asc'; descripcion.push('del más barato'); }
            else if (tiene(PALABRAS_CARO)) { filtrosProducto.orderBy = 'price-desc'; descripcion.push('del más caro'); }
            else if (tiene(PALABRAS_NUEVO)) { filtrosProducto.orderBy = 'newest'; descripcion.push('lo más nuevo'); }

            // "menos de 500" / "hasta 500" -> precio máximo; "más de 200" / "desde 200" -> mínimo.
            var maximo = texto.match(/(menos de|hasta|maximo)\s+(\d+)/);
            var minimo = texto.match(/(mas de|desde|minimo)\s+(\d+)/);
            if (maximo) { filtrosProducto.maxPrice = maximo[2]; descripcion.push('hasta Bs. ' + maximo[2]); }
            if (minimo) { filtrosProducto.minPrice = minimo[2]; descripcion.push('desde Bs. ' + minimo[2]); }

            // Lo que sobra (ej. una marca) se usa como búsqueda de texto.
            var ignorar = PALABRAS_VACIAS.concat(PALABRAS_PRODUCTO, PALABRAS_RUTA, PALABRAS_BARATO,
                PALABRAS_CARO, PALABRAS_OFERTA, PALABRAS_NUEVO);
            var resto = palabras.filter(function (p) {
                return !CATEGORIAS_PRODUCTO[p.norm] && ignorar.indexOf(p.norm) === -1 && !/^\d+$/.test(p.norm);
            }).map(function (p) { return p.orig; }).join(' ');
            if (resto) { filtrosProducto.search = resto; descripcion.push('"' + resto + '"'); }

            return { url: armarUrl(urls.productos, filtrosProducto), mensaje: 'Buscando ' + descripcion.join(' · ') };
        }

        // 3. Rutas.
        var dificultad = null, region = null;
        palabras.forEach(function (p) {
            if (!dificultad && DIFICULTADES[p.norm]) dificultad = DIFICULTADES[p.norm];
            if (!region && REGIONES[p.norm]) region = REGIONES[p.norm];
        });

        if (tiene(PALABRAS_RUTA) || dificultad || region) {
            var filtrosRuta = {};
            var detalle = [];
            if (dificultad) { filtrosRuta.difficulty = dificultad; detalle.push(NOMBRE_DIFICULTAD[dificultad]); }
            if (region) { filtrosRuta.region = region; detalle.push('en ' + region); }

            var ignorarRuta = PALABRAS_VACIAS.concat(PALABRAS_RUTA);
            var restoRuta = palabras.filter(function (p) {
                return ignorarRuta.indexOf(p.norm) === -1 && !DIFICULTADES[p.norm] && !REGIONES[p.norm];
            }).map(function (p) { return p.orig; }).join(' ');
            if (restoRuta) { filtrosRuta.search = restoRuta; detalle.push('"' + restoRuta + '"'); }

            return {
                url: armarUrl(urls.rutas, filtrosRuta),
                mensaje: detalle.length ? 'Buscando rutas ' + detalle.join(' · ') : 'Abriendo rutas'
            };
        }

        // 4. No se entendió: no se navega.
        return null;
    }

    // Arma "/Products?category=...&orderBy=..." codificando cada valor.
    function armarUrl(base, filtros) {
        var partes = Object.keys(filtros).map(function (k) {
            return encodeURIComponent(k) + '=' + encodeURIComponent(filtros[k]);
        });
        return partes.length ? base + '?' + partes.join('&') : base;
    }


    // ---------------------------------------------------------------
    // INTERFAZ: botón flotante + panel con lo que se entendió
    // ---------------------------------------------------------------
    var Reconocimiento = window.SpeechRecognition || window.webkitSpeechRecognition;

    function iniciarInterfaz() {
        var raiz = document.getElementById('asistenteVoz');
        if (!raiz) return;

        // Navegador sin reconocimiento de voz (Firefox, Safari): no se muestra nada.
        if (!Reconocimiento) { raiz.remove(); return; }
        raiz.hidden = false;

        var boton = raiz.querySelector('.asistente-voz__btn');
        var panel = raiz.querySelector('.asistente-voz__panel');
        var estado = raiz.querySelector('.asistente-voz__estado');
        var dicho = raiz.querySelector('.asistente-voz__dicho');

        // Las URLs vienen del servidor (Url.Action en _AsistenteVoz.cshtml).
        var urls = {
            inicio: raiz.dataset.urlInicio,
            rutas: raiz.dataset.urlRutas,
            productos: raiz.dataset.urlProductos,
            carrito: raiz.dataset.urlCarrito,
            favoritos: raiz.dataset.urlFavoritos,
            pedidos: raiz.dataset.urlPedidos,
            reservas: raiz.dataset.urlReservas
        };

        var reconocimiento = null;
        var temporizador = null;

        // Muestra el panel con un estado ("Te escucho...") y un texto debajo.
        function mostrar(textoEstado, textoDicho) {
            clearTimeout(temporizador);
            estado.textContent = textoEstado;
            dicho.textContent = textoDicho || '';
            panel.hidden = false;
        }

        function ocultarEn(ms) {
            clearTimeout(temporizador);
            temporizador = setTimeout(function () { panel.hidden = true; }, ms);
        }

        function escuchar() {
            // Si el dictado de algún campo estaba activo, se detiene (solo un micrófono a la vez).
            document.dispatchEvent(new CustomEvent('voz:detener'));

            var rec = new Reconocimiento();
            rec.lang = 'es-ES';
            rec.continuous = false;
            rec.interimResults = true; // muestra lo que va entendiendo mientras habla

            var textoFinal = '';
            var huboError = false;

            rec.onstart = function () {
                raiz.classList.add('asistente-voz--escuchando');
                mostrar('Te escucho…', 'Prueba: "rutas en Cochabamba" o "botas baratas"');
            };

            rec.onresult = function (evento) {
                var texto = '';
                for (var i = 0; i < evento.results.length; i++) {
                    texto += evento.results[i][0].transcript;
                    if (evento.results[i].isFinal) textoFinal = texto.trim();
                }
                mostrar('Te escucho…', '"' + texto.trim() + '"');
            };

            rec.onerror = function (evento) {
                huboError = true;
                if (evento.error === 'not-allowed' || evento.error === 'service-not-allowed') {
                    mostrar('Sin acceso al micrófono', 'Permite el micrófono en tu navegador para usar el asistente.');
                } else if (evento.error === 'no-speech') {
                    mostrar('No te escuché', 'Toca el micrófono y vuelve a intentarlo.');
                } else if (evento.error !== 'aborted') {
                    mostrar('Algo falló', 'Vuelve a intentarlo en un momento.');
                }
                ocultarEn(4000);
            };

            rec.onend = function () {
                raiz.classList.remove('asistente-voz--escuchando');
                reconocimiento = null;
                if (huboError) return;

                if (!textoFinal) {
                    mostrar('No te escuché', 'Toca el micrófono y vuelve a intentarlo.');
                    ocultarEn(4000);
                    return;
                }

                var resultado = interpretar(textoFinal, urls);
                if (!resultado) {
                    mostrar('No te entendí', 'Prueba: "ir a rutas", "ruta a Corani" o "mochilas en oferta".');
                    ocultarEn(6000);
                    return;
                }

                // Se muestra qué se va a hacer y se navega tras una pausa corta para poder leerlo.
                mostrar(resultado.mensaje, '"' + textoFinal + '"');
                setTimeout(function () { window.location.href = resultado.url; }, 900);
            };

            reconocimiento = rec;
            rec.start();
        }

        boton.addEventListener('click', function () {
            // Segundo toque mientras escucha: detener.
            if (reconocimiento) { reconocimiento.stop(); return; }
            escuchar();
        });

        // Si otro micrófono del sitio empieza a escuchar, este se detiene.
        document.addEventListener('voz:detener', function () {
            if (reconocimiento) reconocimiento.abort();
        });

        // Escape cierra el panel.
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') panel.hidden = true;
        });
    }

    document.addEventListener('DOMContentLoaded', iniciarInterfaz);

    // Expuesto solo para probar en la consola del navegador.
    window.AsistenteVoz = { interpretar: interpretar };
})();
