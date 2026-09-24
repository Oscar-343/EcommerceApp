/**
 * Dictado por voz simple para campos de texto, usando la Web Speech API
 * nativa del navegador (sin backend, sin costo). Cliente-only.
 *
 * Soporte: Chrome, Edge, Brave (desktop y Android). No funciona en Firefox
 * ni en la mayoría de Safari — si el navegador no expone SpeechRecognition,
 * este script no toca el DOM (sin botones rotos).
 */
(function () {
    var SpeechRecognitionCtor = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognitionCtor) return;

    var FIELD_SELECTOR = 'input[type="text"], input:not([type]), input[type="email"], ' +
        'input[type="tel"], input[type="search"], textarea';

    var activeButton = null;
    var activeRecognition = null;

    function stopActive() {
        if (activeRecognition) {
            try { activeRecognition.stop(); } catch (e) { /* noop */ }
        }
    }

    function setActive(btn, isActive) {
        btn.classList.toggle('dictation-btn--active', isActive);
    }

    function startDictation(field, btn) {
        // Avisa a otros micrófonos del sitio (ej. el asistente) para que se detengan.
        document.dispatchEvent(new CustomEvent('voz:detener'));

        if (activeButton && activeButton !== btn) {
            stopActive();
        }

        var recognition = new SpeechRecognitionCtor();
        recognition.lang = 'es-ES';
        recognition.continuous = false;
        recognition.interimResults = false;

        recognition.onstart = function () {
            activeButton = btn;
            activeRecognition = recognition;
            setActive(btn, true);
        };

        recognition.onresult = function (event) {
            var transcript = event.results[0][0].transcript.trim();
            if (!transcript) return;
            var current = field.value.trim();
            field.value = current ? current + ' ' + transcript : transcript;
            field.dispatchEvent(new Event('input', { bubbles: true }));
            field.dispatchEvent(new Event('change', { bubbles: true }));
        };

        recognition.onend = function () {
            setActive(btn, false);
            if (activeRecognition === recognition) {
                activeButton = null;
                activeRecognition = null;
            }
        };

        recognition.onerror = function () {
            setActive(btn, false);
            if (activeRecognition === recognition) {
                activeButton = null;
                activeRecognition = null;
            }
        };

        recognition.start();
    }

    function wrapField(field) {
        var wrap = document.createElement('span');
        wrap.className = 'dictation-wrap';
        field.parentNode.insertBefore(wrap, field);
        wrap.appendChild(field);

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'dictation-btn';
        btn.setAttribute('aria-label', 'Dictar por voz');
        btn.innerHTML = '<i class="fas fa-microphone"></i>';
        wrap.appendChild(btn);

        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            if (btn === activeButton) {
                stopActive();
                return;
            }

            startDictation(field, btn);
        });

        field.dataset.dictationProcessed = 'true';
    }

    function scan(root) {
        var fields = (root || document).querySelectorAll(FIELD_SELECTOR);
        fields.forEach(function (field) {
            if (field.dataset.dictationProcessed) return;
            wrapField(field);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        scan();

        var observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType !== 1) return;
                    if (node.matches && node.matches(FIELD_SELECTOR) && !node.dataset.dictationProcessed) {
                        wrapField(node);
                    }
                    if (node.querySelectorAll) {
                        scan(node);
                    }
                });
            });
        });
        observer.observe(document.body, { childList: true, subtree: true });

        // Si otro micrófono empieza a escuchar, se corta este dictado.
        document.addEventListener('voz:detener', function () {
            if (activeRecognition) activeRecognition.abort();
        });
    });
})();
