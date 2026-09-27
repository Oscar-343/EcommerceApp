// ============================================
// CHECKOUT — stepper, validaciones y tarjeta de vista previa.
// Pago simulado: los datos de la tarjeta nunca salen del navegador
// (sus inputs no tienen atributo name) y nunca se escriben en console.log.
// Se completa en las Fases 7 a 9.
// ============================================

(function () {
    'use strict';

    // ---------- Resumen colapsable (solo se ve el botón por debajo de 992px) ----------
    const resumen = document.querySelector('.checkout-summary');
    const btnResumen = document.querySelector('.checkout-summary__toggle');
    if (resumen && btnResumen) {
        btnResumen.addEventListener('click', function () {
            const abierto = resumen.classList.toggle('is-open');
            btnResumen.setAttribute('aria-expanded', String(abierto));
        });
    }

    const form = document.getElementById('checkout-form');
    if (!form) return;

    const pasoEntrega = document.getElementById('paso-entrega');
    const pasoPago = document.getElementById('paso-pago');
    const camposEntrega = document.getElementById('entrega-campos');
    const resumenEntrega = document.getElementById('entrega-resumen');
    const btnContinuar = document.getElementById('entrega-continuar');
    const btnEditar = document.getElementById('entrega-editar');

    // Validador de jQuery Validation con las reglas data-val del formulario.
    // Se pide recién al usarlo: si se crea antes de que el script "unobtrusive" lea los
    // atributos data-val (lo hace al terminar de cargar la página), queda sin reglas.
    function validador() {
        return $(form).validate();
    }

    // ---------- Stepper ----------
    // Marca como completados los pasos anteriores a "actual" y como pendientes los siguientes.
    function marcarPaso(actual) {
        document.querySelectorAll('.checkout-step[data-step]').forEach(function (paso) {
            const numero = Number(paso.dataset.step);
            const circulo = paso.querySelector('.checkout-step__circle');

            paso.classList.toggle('is-done', numero < actual);
            paso.classList.toggle('is-current', numero === actual);

            if (numero === actual) paso.setAttribute('aria-current', 'step');
            else paso.removeAttribute('aria-current');

            circulo.innerHTML = numero < actual
                ? '<i class="fa-solid fa-check" aria-hidden="true"></i>'
                : String(numero);
        });
    }

    // Muestra una sección con la animación de aparición (fade + 12px hacia arriba).
    function mostrar(elemento) {
        elemento.hidden = false;
        elemento.classList.remove('checkout-reveal');
        void elemento.offsetWidth; // reinicia la animación si ya se había mostrado antes
        elemento.classList.add('checkout-reveal');
    }

    // ---------- Paso 2: datos de entrega ----------
    function valor(nombre) {
        const campo = form.elements[nombre];
        return campo ? campo.value.trim() : '';
    }

    // Valida solo los campos de esta sección. Devuelve true si todos están bien.
    function entregaValida() {
        const validator = validador();
        let valida = true;
        camposEntrega.querySelectorAll('input, textarea').forEach(function (campo) {
            if (validator.element(campo) === false) valida = false;
        });
        return valida;
    }

    // Colapsa la sección de entrega y abre la de pago.
    function irAPago(desplazar) {
        // Línea de resumen: "Juan Pérez · Cochabamba · Av. Siempre Viva 123".
        // textContent (no innerHTML) para que lo escrito por el usuario nunca se interprete como HTML.
        resumenEntrega.textContent = [valor('NombreCompleto'), valor('Ciudad'), valor('Direccion')].join(' · ');

        camposEntrega.hidden = true;
        resumenEntrega.hidden = false;
        btnEditar.hidden = false;
        pasoEntrega.classList.add('is-collapsed');

        marcarPaso(3);
        mostrar(pasoPago);
        if (desplazar) {
            // El desplazamiento suave también respeta la preferencia de "reducir movimiento".
            const sinMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            pasoPago.scrollIntoView({ behavior: sinMovimiento ? 'auto' : 'smooth', block: 'start' });
        }
    }

    // Vuelve a abrir la sección de entrega para corregir algo.
    function editarEntrega() {
        pasoPago.hidden = true;
        resumenEntrega.hidden = true;
        btnEditar.hidden = true;
        pasoEntrega.classList.remove('is-collapsed');

        marcarPaso(2);
        mostrar(camposEntrega);
        form.elements['NombreCompleto'].focus();
    }

    btnContinuar.addEventListener('click', function () {
        if (entregaValida()) {
            irAPago(true);
        } else {
            // Lleva el foco al primer campo con error para que el usuario vea qué falta.
            const primerError = camposEntrega.querySelector('.input-validation-error');
            if (primerError) primerError.focus();
        }
    });

    btnEditar.addEventListener('click', editarEntrega);

    // ---------- Paso 3: método de pago ----------
    const radiosMetodo = form.querySelectorAll('input[name="MetodoPago"]');
    const paneles = pasoPago.querySelectorAll('.checkout-panel');
    const btnPagar = document.getElementById('btn-pagar');
    const procesando = document.getElementById('checkout-procesando');

    const tarjeta = {
        numero: document.getElementById('tarjeta-numero'),
        nombre: document.getElementById('tarjeta-nombre'),
        vence: document.getElementById('tarjeta-vence'),
        cvv: document.getElementById('tarjeta-cvv')
    };
    const preview = {
        caja: document.getElementById('tarjeta-preview'),
        numero: document.getElementById('preview-numero'),
        nombre: document.getElementById('preview-nombre'),
        vence: document.getElementById('preview-vence'),
        cvv: document.getElementById('preview-cvv'),
        marca: document.getElementById('preview-marca')
    };
    const qrConfirmado = document.getElementById('qr-confirmado');
    const transferenciaConfirmada = document.getElementById('transferencia-confirmado');

    function metodoElegido() {
        const radio = form.querySelector('input[name="MetodoPago"]:checked');
        return radio ? radio.value : '';
    }

    function soloDigitos(texto) {
        return texto.replace(/\D/g, '');
    }

    // Marca por prefijo: 4 → VISA; 51–55 o 2221–2720 → MASTERCARD; otro → sin marca.
    function detectarMarca(digitos) {
        if (/^4/.test(digitos)) return 'VISA';
        const dos = Number(digitos.slice(0, 2));
        const cuatro = Number(digitos.slice(0, 4));
        if (dos >= 51 && dos <= 55) return 'MASTERCARD';
        if (digitos.length >= 4 && cuatro >= 2221 && cuatro <= 2720) return 'MASTERCARD';
        return '';
    }

    // Algoritmo de Luhn: detecta errores de tipeo en números de tarjeta.
    // De derecha a izquierda se duplica uno de cada dos dígitos (si da más de 9, se le resta 9)
    // y se suma todo. El número es válido si la suma termina en 0 (es múltiplo de 10).
    function luhnValido(digitos) {
        let suma = 0;
        let duplicar = false;
        for (let i = digitos.length - 1; i >= 0; i--) {
            let d = Number(digitos[i]);
            if (duplicar) {
                d *= 2;
                if (d > 9) d -= 9;
            }
            suma += d;
            duplicar = !duplicar;
        }
        return suma % 10 === 0;
    }

    // Cada validación devuelve el mensaje de error, o '' si el campo está bien.
    function errorNumero() {
        const digitos = soloDigitos(tarjeta.numero.value);
        if (digitos.length === 0) return 'Ingresa el número de la tarjeta.';
        if (digitos.length !== 16 || !luhnValido(digitos)) return 'Número de tarjeta no válido.';
        return '';
    }

    function errorNombre() {
        const nombre = tarjeta.nombre.value.trim();
        if (nombre.length === 0) return 'Ingresa el nombre del titular.';
        if (nombre.length < 3) return 'El nombre es demasiado corto.';
        return '';
    }

    // Vencimiento MM/AA: mes 01–12 y que no haya pasado (la tarjeta vale hasta el último día de ese mes).
    function errorVence() {
        const partes = /^(\d{2})\/(\d{2})$/.exec(tarjeta.vence.value);
        if (!partes) return 'Usa el formato MM/AA.';
        const mes = Number(partes[1]);
        const anio = 2000 + Number(partes[2]);
        if (mes < 1 || mes > 12) return 'El mes debe estar entre 01 y 12.';
        const hoy = new Date();
        const mesActual = hoy.getFullYear() * 12 + hoy.getMonth() + 1;
        if (anio * 12 + mes < mesActual) return 'La tarjeta está vencida.';
        return '';
    }

    function errorCvv() {
        return /^\d{3}$/.test(tarjeta.cvv.value) ? '' : 'El CVV tiene 3 dígitos.';
    }

    const validaciones = [
        [tarjeta.numero, errorNumero],
        [tarjeta.nombre, errorNombre],
        [tarjeta.vence, errorVence],
        [tarjeta.cvv, errorCvv]
    ];

    // Muestra (o limpia) el error debajo de un campo de tarjeta.
    function mostrarError(campo, mensaje) {
        document.getElementById(campo.id + '-error').textContent = mensaje;
        campo.classList.toggle('input-validation-error', mensaje !== '');
        campo.setAttribute('aria-invalid', mensaje !== '' ? 'true' : 'false');
    }

    function tarjetaValida() {
        return validaciones.every(function (v) { return v[1]() === ''; });
    }

    // El botón "Pagar" solo se habilita cuando el método elegido está completo.
    function metodoValido() {
        switch (metodoElegido()) {
            case 'Tarjeta': return tarjetaValida();
            case 'QR': return qrConfirmado.checked;
            case 'Transferencia': return transferenciaConfirmada.checked;
            default: return false;
        }
    }

    function actualizarBoton() {
        btnPagar.disabled = !metodoValido();
    }

    // Muestra solo el panel del método elegido (con fade).
    function mostrarPanel() {
        const metodo = metodoElegido();
        paneles.forEach(function (panel) {
            if (panel.dataset.panel === metodo) {
                if (panel.hidden) mostrar(panel);
            } else {
                panel.hidden = true;
            }
        });
        actualizarBoton();
    }

    // ----- Máscaras y vista previa en vivo -----
    tarjeta.numero.addEventListener('input', function () {
        const digitos = soloDigitos(tarjeta.numero.value).slice(0, 16);
        // Espacio cada 4 dígitos: "4242 4242 4242 4242"
        tarjeta.numero.value = digitos.replace(/(\d{4})(?=\d)/g, '$1 ');

        preview.marca.textContent = detectarMarca(digitos);
        preview.numero.textContent = (digitos + '•'.repeat(16 - digitos.length)).replace(/(.{4})(?=.)/g, '$1 ');
    });

    tarjeta.nombre.addEventListener('input', function () {
        preview.nombre.textContent = tarjeta.nombre.value.trim().toUpperCase() || 'NOMBRE APELLIDO';
    });

    tarjeta.vence.addEventListener('input', function (evento) {
        let digitos = soloDigitos(tarjeta.vence.value).slice(0, 4);
        // Si escribe un solo dígito mayor que 1 (ej. "4"), se entiende como mes "04".
        if (digitos.length === 1 && Number(digitos) > 1) digitos = '0' + digitos;
        // Agrega la barra al escribir, pero deja borrarla con la tecla de retroceso.
        const borrando = evento.inputType === 'deleteContentBackward';
        tarjeta.vence.value = digitos.length > 2 || (digitos.length === 2 && !borrando)
            ? digitos.slice(0, 2) + '/' + digitos.slice(2)
            : digitos;
        preview.vence.textContent = tarjeta.vence.value || 'MM/AA';
    });

    tarjeta.cvv.addEventListener('input', function () {
        tarjeta.cvv.value = soloDigitos(tarjeta.cvv.value).slice(0, 3);
        // En la vista previa se muestra oculto: un punto por dígito.
        preview.cvv.textContent = '•'.repeat(tarjeta.cvv.value.length) || '•••';
    });

    // Al enfocar el CVV la tarjeta gira y muestra el reverso; al salir, vuelve.
    tarjeta.cvv.addEventListener('focus', function () { preview.caja.classList.add('is-flipped'); });
    tarjeta.cvv.addEventListener('blur', function () { preview.caja.classList.remove('is-flipped'); });

    // Errores: se muestran al salir del campo y, desde ahí, se actualizan mientras escribe.
    validaciones.forEach(function (v) {
        const campo = v[0];
        const validar = v[1];
        campo.addEventListener('blur', function () {
            if (campo.value !== '') mostrarError(campo, validar());
        });
        campo.addEventListener('input', function () {
            if (campo.classList.contains('input-validation-error')) mostrarError(campo, validar());
            actualizarBoton();
        });
    });

    radiosMetodo.forEach(function (radio) { radio.addEventListener('change', mostrarPanel); });
    qrConfirmado.addEventListener('change', actualizarBoton);
    transferenciaConfirmada.addEventListener('change', actualizarBoton);

    // Botones "Copiar" de la transferencia.
    pasoPago.querySelectorAll('.checkout-copiar').forEach(function (boton) {
        boton.addEventListener('click', function () {
            if (!navigator.clipboard) return;
            navigator.clipboard.writeText(boton.dataset.copiar).then(function () {
                const texto = boton.querySelector('.checkout-copiar__texto');
                texto.textContent = 'Copiado';
                setTimeout(function () { texto.textContent = 'Copiar'; }, 2000);
            });
        });
    });

    // ---------- Envío ----------
    form.addEventListener('submit', function (evento) {
        // Enter en un campo de entrega haría submit del formulario completo: mientras el paso
        // de pago no esté abierto, el submit se convierte en "Continuar al pago".
        if (pasoPago.hidden) {
            evento.preventDefault();
            btnContinuar.click();
            return;
        }

        if (!metodoValido()) {
            evento.preventDefault();
            // Muestra qué falta en la tarjeta (en QR y transferencia falta marcar el check).
            if (metodoElegido() === 'Tarjeta') {
                validaciones.forEach(function (v) { mostrarError(v[0], v[1]()); });
            }
            return;
        }

        // De la tarjeta solo viajan la marca y los últimos 4 dígitos (el servidor los vuelve a validar).
        const esTarjeta = metodoElegido() === 'Tarjeta';
        const digitos = soloDigitos(tarjeta.numero.value);
        document.getElementById('TarjetaMarca').value = esTarjeta ? detectarMarca(digitos) : '';
        document.getElementById('TarjetaUltimos4').value = esTarjeta ? digitos.slice(-4) : '';

        // jQuery Validation revisa el formulario después de este listener: si algo no pasa, no se muestra el overlay.
        if (!$(form).valid()) return;

        // Evita el doble clic y muestra "Procesando tu pago…" mientras el servidor responde.
        btnPagar.disabled = true;
        procesando.hidden = false;
    });

    // Si el usuario vuelve con "Atrás" y el navegador restaura la página guardada,
    // se quita el overlay y el botón vuelve a su estado real.
    window.addEventListener('pageshow', function (evento) {
        if (evento.persisted) {
            procesando.hidden = true;
            actualizarBoton();
        }
    });

    // ---------- Estado inicial ----------
    mostrarPanel();

    // Si la página vuelve del servidor (p. ej. pago rechazado) y la entrega no tiene errores,
    // se abre directamente el paso de pago.
    if (form.dataset.enviado === 'true' && !camposEntrega.querySelector('.field-validation-error')) {
        irAPago(false);
    }
})();
