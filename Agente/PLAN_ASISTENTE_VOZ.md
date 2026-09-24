# Plan: Asistente de voz — Tren al Sur

> **Para Claude Code.** Lee este archivo completo antes de empezar.
> El proyecto es universitario y el dueño lo va a **estudiar después**. Manda la simplicidad sobre la elegancia.

## Objetivo

Un botón flotante con micrófono, abajo a la derecha, con estilo outdoor. Al tocarlo escucha lo que el cliente quiere hacer y lo lleva a la página correspondiente, ya filtrada:

| El cliente dice | Resultado |
|---|---|
| "ir a rutas" | `/Services` |
| "buscar una ruta a Corani" | `/Services?search=corani` |
| "rutas fáciles en Cochabamba" | `/Services?difficulty=easy&region=Cochabamba` |
| "botas de senderismo baratas" | `/Products?category=Calzado de trekking&orderBy=price-asc` |
| "mochilas en oferta" | `/Products?category=Mochilas&onlyOffers=true` |
| "chaquetas de menos de 500 bolivianos" | `/Products?category=Ropa outdoor&maxPrice=500` |
| "mochila Deuter" | `/Products?category=Mochilas&search=deuter` |
| "llévame al carrito" / "mis reservas" | `/Cart` / `/Reservations` |
| "hola, cómo estás" | No navega: muestra ejemplos de qué decir |

## Decisiones ya tomadas (no las cambies sin preguntarme)

1. **Web Speech API del navegador**, igual que el dictado de formularios (`Agente/tren-al-sur-dictado.md`). Sin backend, sin API key, sin costo.
2. **La intención se interpreta con palabras clave en JavaScript**, no con IA. Es predecible, gratis y se puede estudiar. Lo que no se entiende no navega: muestra ejemplos.
3. **No se crean endpoints nuevos.** El asistente solo arma URLs con los filtros que `ProductsController.Index` y `ServicesController.Index` ya aceptan.
4. **Solo en el sitio público** (`_Layout.cshtml`). No va en el admin ni en login/registro.
5. **Sin soporte, no aparece.** En Firefox y la mayoría de Safari no existe `SpeechRecognition`: el botón no se muestra (nada roto).
6. **Un solo micrófono a la vez.** El asistente y el dictado de campos se avisan con el evento `voz:detener`.
7. **Sin respuesta hablada** (`speechSynthesis`). El asistente responde con texto en un panel.

## Reglas

1. **Código simple.** Sin librerías nuevas. Comentarios breves en español solo donde expliquen el *por qué*.
2. Respeta `Agente/AGENTS.md` y `Agente/.agent/*.md`. Si este plan los contradice, avísame antes de actuar.
3. **No inventes funcionalidades** fuera de este plan. Si algo requiere una decisión de diseño, pregúntame.
4. No toques controladores, modelos, migraciones ni `appsettings*.json`. Este plan es **solo frontend**.
5. Rama `asistente-voz` creada desde la rama donde esté integrado `home-mapa` (pregúntame cuál si no está claro). **Un commit por fase. No hagas push.**
6. Después de cada fase: `dotnet build` sin errores y dime qué probar a mano.

---

## Fase 1 — Script del asistente

### Archivo nuevo: `wwwroot/js/asistente-voz.js`

Tiene dos partes separadas a propósito:

- **`interpretar(texto, urls)`**: recibe lo dicho y devuelve `{ url, mensaje }` o `null`. No toca la página, así que se puede probar en la consola.
- **Interfaz**: el botón, el panel de mensajes y el reconocimiento de voz.

Orden de interpretación (importa):

1. **Páginas del usuario**: carrito, favoritos, pedidos, reservas, inicio.
2. **Productos**: si se nombra una categoría ("botas", "mochila"…) o una palabra de tienda ("productos", "equipo"). Va **antes** que rutas porque "botas de senderismo" contiene "senderismo" pero se refiere a un producto.
3. **Rutas**: palabras de ruta ("ruta", "trekking", "caminata"…), una dificultad o una región.
4. **Nada coincide**: `null`, no se navega.

Las palabras se comparan **sin tildes**, pero lo que se envía como búsqueda conserva las tildes originales ("Pantalón" se busca como "pantalón").

Copia este archivo tal cual:

```javascript
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
```

- Los valores de `CATEGORIAS_PRODUCTO` deben coincidir **exactamente** con `ProductCategories.All`, los de `DIFICULTADES` con `RouteDifficulty` y los de `REGIONES` con `RouteRegions.All`. **Verifícalo contra los modelos** antes del commit; si alguno no coincide, corrige el JavaScript, no los modelos.
- Idioma: `es-ES`, igual que `dictation.js`. No lo cambies sin probar en vivo.

**Probar (Fase 1):** con el sitio abierto en Chrome, en la consola:

```javascript
const urls = { inicio: '/', rutas: '/Services', productos: '/Products', carrito: '/Cart',
               favoritos: '/Favorites', pedidos: '/Orders', reservas: '/Reservations' };
AsistenteVoz.interpretar('botas de senderismo baratas', urls);
AsistenteVoz.interpretar('buscar una ruta a Corani', urls);
AsistenteVoz.interpretar('quiero reservar la ruta de Corani', urls); // debe ir a rutas, no a "mis reservas"
AsistenteVoz.interpretar('hola cómo estás', urls);                   // debe dar null
```

Los resultados deben coincidir con la tabla del objetivo.

---

## Fase 2 — Botón, estilos y conexión al layout

### Archivo nuevo: `Views/Shared/_AsistenteVoz.cshtml`

Las URLs se generan con `Url.Action` para que no queden escritas a mano en el JavaScript. El anillo SVG imita el bisel de una brújula (cuatro marcas en los puntos cardinales): es el toque outdoor del botón.

```cshtml
@* Asistente de voz: botón flotante abajo a la derecha.
   Empieza oculto; asistente-voz.js lo muestra solo si el navegador reconoce voz. *@
<div id="asistenteVoz" class="asistente-voz" hidden
     data-url-inicio="@Url.Action("Index", "Home")"
     data-url-rutas="@Url.Action("Index", "Services")"
     data-url-productos="@Url.Action("Index", "Products")"
     data-url-carrito="@Url.Action("Index", "Cart")"
     data-url-favoritos="@Url.Action("Index", "Favorites")"
     data-url-pedidos="@Url.Action("Index", "Orders")"
     data-url-reservas="@Url.Action("Index", "Reservations")">

    <div class="asistente-voz__panel" role="status" aria-live="polite" hidden>
        <p class="asistente-voz__estado"></p>
        <p class="asistente-voz__dicho"></p>
    </div>

    <button type="button" class="asistente-voz__btn" aria-label="Asistente de voz: toca y dime qué buscas">
        <!-- Anillo tipo brújula: círculo punteado + 4 marcas cardinales -->
        <svg class="asistente-voz__anillo" viewBox="0 0 64 64" aria-hidden="true">
            <circle cx="32" cy="32" r="29" fill="none" stroke="currentColor" stroke-width="1" stroke-dasharray="2 4" />
            <path d="M32 1v6 M32 57v6 M1 32h6 M57 32h6" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
        <i class="fas fa-microphone"></i>
    </button>
</div>
```

### Archivo nuevo: `wwwroot/css/asistente-voz.css`

Usa solo las variables de `tokens.css`. La animación respeta `prefers-reduced-motion`, que ya está configurado de forma global en `tokens.css`.

```css
/* =========================================================
   ASISTENTE DE VOZ — botón flotante con anillo de brújula
   ========================================================= */
.asistente-voz {
    position: fixed;
    right: 1.5rem;
    bottom: 1.5rem;
    z-index: 998; /* debajo de overlays (filtros móviles usan 999) */
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.75rem;
}

.asistente-voz[hidden],
.asistente-voz__panel[hidden] {
    display: none;
}

/* --- Botón --- */
.asistente-voz__btn {
    position: relative;
    width: 60px;
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    border: 1px solid var(--color-border);
    background: var(--color-card);
    color: var(--color-text);
    font-size: 1.2rem;
    cursor: pointer;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
    transition: transform 0.3s ease, background 0.3s ease, border-color 0.3s ease;
}

.asistente-voz__btn:hover {
    transform: translateY(-3px);
    border-color: var(--color-green-accent);
}

.asistente-voz__btn:focus-visible {
    outline: 2px solid var(--color-green-accent);
    outline-offset: 3px;
}

.asistente-voz__anillo {
    position: absolute;
    inset: 4px;
    width: calc(100% - 8px);
    height: calc(100% - 8px);
    color: var(--color-text-secondary);
    opacity: 0.6;
    pointer-events: none;
}

/* --- Escuchando: el anillo gira como una brújula y sale una onda suave --- */
.asistente-voz--escuchando .asistente-voz__btn {
    background: var(--color-green);
    border-color: var(--color-green-accent);
}

.asistente-voz--escuchando .asistente-voz__anillo {
    color: var(--color-text);
    opacity: 1;
    animation: asistente-voz-girar 8s linear infinite;
}

.asistente-voz--escuchando .asistente-voz__btn::after {
    content: '';
    position: absolute;
    inset: -1px;
    border-radius: 50%;
    border: 1px solid var(--color-green-accent);
    animation: asistente-voz-onda 1.8s ease-out infinite;
}

@keyframes asistente-voz-girar {
    to { transform: rotate(360deg); }
}

@keyframes asistente-voz-onda {
    from { transform: scale(1); opacity: 0.8; }
    to { transform: scale(1.7); opacity: 0; }
}

/* --- Panel de mensajes --- */
.asistente-voz__panel {
    max-width: 280px;
    padding: 0.8rem 1rem;
    border: 1px solid var(--color-border);
    border-radius: 12px;
    background: rgba(23, 30, 26, 0.95);
    backdrop-filter: blur(8px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
    animation: asistente-voz-aparecer 0.3s ease-out;
}

.asistente-voz__panel p {
    margin: 0;
}

.asistente-voz__estado {
    font-size: 0.72rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--color-green-accent);
}

.asistente-voz__dicho {
    margin-top: 0.3rem !important;
    font-size: 0.9rem;
    line-height: 1.4;
    color: var(--color-text);
}

@keyframes asistente-voz-aparecer {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0); }
}

/* --- Móvil --- */
@media (max-width: 768px) {
    .asistente-voz {
        right: 1rem;
        bottom: 1rem;
    }

    .asistente-voz__btn {
        width: 56px;
        height: 56px;
    }

    /* El Home tiene una barra inferior fija de 64px: el botón se ubica encima */
    body:has(.mobile-bottom-nav) .asistente-voz {
        bottom: calc(64px + 1rem);
    }
}
```

### `Views/Shared/_Layout.cshtml`

1. En el `<head>`, junto a `dictation.css`:
   ```html
   <link rel="stylesheet" href="~/css/asistente-voz.css" asp-append-version="true" />
   ```
2. Justo antes de los `<script>` del final del `<body>`:
   ```cshtml
   <partial name="_AsistenteVoz" />
   ```
3. Junto a `dictation.js`:
   ```html
   <script src="~/js/asistente-voz.js" asp-append-version="true"></script>
   ```

**No** lo agregues a `_AdminLayout.cshtml` ni a `_AuthLayout.cshtml`.

---

## Fase 3 — Un solo micrófono a la vez (`dictation.js`)

El asistente ya emite `voz:detener` antes de escuchar y se detiene si lo recibe. Falta que el dictado de campos haga lo mismo. Dos cambios pequeños en `wwwroot/js/dictation.js`:

1. Al inicio de `startDictation`, antes de crear el reconocimiento:
   ```javascript
   // Avisa a otros micrófonos del sitio (ej. el asistente) para que se detengan.
   document.dispatchEvent(new CustomEvent('voz:detener'));
   ```
2. Dentro del `DOMContentLoaded`, junto al `MutationObserver`:
   ```javascript
   // Si otro micrófono empieza a escuchar, se corta este dictado.
   document.addEventListener('voz:detener', function () {
       if (activeRecognition) activeRecognition.abort();
   });
   ```

Ojo con el orden: el `dispatchEvent` del punto 1 va **antes** de asignar `activeRecognition` a la nueva instancia, para que el dictado no se corte a sí mismo.

---

## Fase 4 — Documentación

- Crea `Agente/tren-al-sur-asistente-voz.md` con un resumen corto: qué hace, cómo se interpreta (orden de prioridad), cómo agregar palabras nuevas a los diccionarios y sus limitaciones.
- Anota el asistente en `Agente/.agent/PROJECT_STATE.md`.

---

## Pruebas a mano (Chrome, con micrófono)

- [ ] El botón aparece abajo a la derecha en Inicio, Rutas, Productos y Detalle de producto.
- [ ] En móvil, en el Home queda **encima** de la barra inferior; en las demás páginas, en la esquina.
- [ ] Al tocarlo: se pide permiso de micrófono la primera vez, el botón se pone verde, el anillo gira y el panel dice "Te escucho…".
- [ ] Mientras hablas, el panel muestra lo que va entendiendo.
- [ ] Cada frase de la tabla del objetivo lleva a la URL indicada y la página muestra los filtros aplicados.
- [ ] "hola cómo estás" no navega y muestra ejemplos.
- [ ] Tocar el botón mientras escucha lo detiene.
- [ ] Negar el permiso de micrófono muestra "Sin acceso al micrófono".
- [ ] Con el dictado de un campo activo, tocar el asistente corta el dictado (y al revés).
- [ ] En Firefox el botón no aparece y no hay errores en la consola.
- [ ] El botón no aparece en el panel admin ni en login.
- [ ] Con "reducir movimiento" activado en el sistema, el anillo no gira.

## Limitaciones conocidas (avisarme, no son bugs a resolver)

- Solo Chrome, Edge y Brave. Requiere HTTPS o `localhost`.
- Si la búsqueda combina categoría y texto ("pantalón impermeable"), puede no haber resultados si el texto no aparece en el nombre, la descripción o la marca. La página muestra su estado vacío normal.
- Los números los suele entregar el navegador en cifras ("500"). Si en alguna prueba llegan en palabras ("quinientos"), el filtro de precio no se aplica: avísame.

## Fuera de alcance (no lo hagas en este plan)

- Respuesta hablada (`speechSynthesis`), conversación de varios turnos o IA para interpretar.
- Acciones que modifican datos por voz (agregar al carrito, reservar, pagar).
- Endpoints nuevos o cambios en controladores.
- Escribir en lugar de hablar dentro del asistente (ya existe el buscador del navbar).
