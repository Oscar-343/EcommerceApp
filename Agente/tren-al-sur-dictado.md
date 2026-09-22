# Tren al Sur — Dictado por voz en formularios

## Qué es y por qué esta técnica
Dictado simple (hablás, se escribe tal cual) usando la **Web Speech API**
nativa del navegador (`SpeechRecognition` / `webkitSpeechRecognition`).
100% cliente, sin backend, sin API key, sin costo por uso.

## Limitaciones importantes (avisar al usuario, no son bugs a resolver)
- Funciona en **Chrome, Edge y Brave** (desktop y Android). **No funciona en
  Firefox** (no lo implementa). En Safari es parcial/inconsistente — el
  script debe detectar si el navegador lo soporta y **no mostrar el botón
  de micrófono si no está disponible**, en vez de mostrar un botón roto.
- Requiere **HTTPS** (o `localhost` para desarrollo). En Render esto no es
  problema porque ya sirve por HTTPS, pero avisar que en `localhost:5187`
  con `http` puro podría no pedir permiso de micrófono correctamente —
  confirmar si el dev server usa `https://localhost:xxxx` también.
- El navegador va a pedir permiso de micrófono la primera vez — es un
  permiso del navegador, no hay nada que programar para eso.
- Reconoce en español (`es-ES` o `es-419`, ver abajo) — probar cuál da mejor
  resultado con acento boliviano, no hay forma de saberlo sin probarlo en
  vivo.

## Implementación

### 1. Archivo nuevo: `wwwroot/js/dictation.js`
Un módulo único, cargado globalmente, que:

1. Al cargar la página, revisa si `window.SpeechRecognition ||
   window.webkitSpeechRecognition` existe. Si no existe, no hace nada (sin
   errores en consola, sin botones rotos).
2. Recorre todos los `input[type="text"], input:not([type]),
   input[type="email"], input[type="tel"], input[type="search"], textarea`
   del documento (evitar campos de contraseña, número y fecha — dictar ahí
   no tiene sentido) y le inyecta, justo al lado (o superpuesto en la
   esquina del campo), un botón pequeño de ícono de micrófono
   (`<button type="button" class="dictation-btn"><i class="fas
   fa-microphone"></i></button>`).
3. Al hacer clic en el botón: crea una instancia de
   `SpeechRecognition`/`webkitSpeechRecognition`, `lang = 'es-ES'`,
   `continuous = false`, `interimResults = false`. Mientras escucha, el
   botón cambia a un estado visual "grabando" (ej. clase `.dictation-btn--active`,
   con una animación de pulso). Al recibir el resultado
   (`onresult`), inserta el texto reconocido en el campo asociado — si el
   campo ya tiene contenido, agregarlo con un espacio antes, no
   sobrescribir. Al terminar (`onend`) o si hay error (`onerror`), sacar el
   estado "grabando".
4. Importante: usar un `MutationObserver` (o volver a correr el escaneo
   cuando se abran modales/formularios dinámicos, si el sitio los tiene) 
   para que campos que aparecen después de la carga inicial (formularios en
   modales, campos que se muestran/ocultan con JS) también reciban su botón
   de micrófono.

### 2. CSS nuevo (agregar a `wwwroot/css/site.css`, es global)
Estilo del botón: pequeño, círculo, posicionado dentro del campo (a la
derecha, con el input con `padding-right` suficiente para no tapar texto),
color `var(--color-text-secondary)` en reposo, `var(--color-green-accent)`
+ animación de pulso mientras está "grabando" (`.dictation-btn--active`).
Mantenerlo visualmente discreto — no debe competir con el resto de los
inputs del sitio.

### 3. Conectarlo en `Views/Shared/_Layout.cshtml`
Agregar `<script src="~/js/dictation.js" asp-append-version="true"></script>`
junto a los demás scripts globales, **una sola vez**, para que aplique a
todo el sitio sin tocar cada vista individualmente.

## Qué NO hacer
- No crear un botón de micrófono por formulario manualmente en cada vista
  — el objetivo es un solo script que cubra todo el sitio sin mantenimiento
  por página.
- No usar ningún servicio de transcripción por API (Whisper, Google
  Speech-to-Text, etc.) — no hace falta, y eso sí tendría costo y
  requeriría backend. Esto es dictado simple, alcanza con el navegador.

## Cómo probarlo (con Claude Code, en vivo)
1. `dotnet watch` y abrir el sitio en Chrome.
2. Probar el botón en al menos 3 formularios distintos de distinta parte
   del sitio (ej: crear producto en Admin, formulario de reserva de ruta,
   Contacto si existe) para confirmar que el script realmente cubre todo
   el sitio sin tener que tocarlos uno por uno.
3. Confirmar que el permiso de micrófono se pide correctamente y que el
   texto dictado aparece en el campo correcto.
4. Abrir el sitio en Firefox y confirmar que **no aparece ningún botón
   roto** (el feature-detect debe ocultarlo silenciosamente).
