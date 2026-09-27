/* =========================================================
   ABASTECER TIENDA — formulario dinámico (Views/Abastecimientos/Nuevo)
   - Al elegir el proveedor se cargan por AJAX solo los productos de sus marcas.
   - Cada producto elegido es una fila con inputs Lineas[i].ProductoId / Cantidad / CostoUnitario,
     que el model binding de ASP.NET convierte en la lista AbastecimientoFormViewModel.Lineas.
   - Los subtotales y el total son solo una vista previa: el servidor recalcula y valida todo.
   ========================================================= */
$(function () {
    const $form = $('#formAbastecimiento');
    if (!$form.length) return;

    const urlProductos = $form.data('url-productos');
    const lineasIniciales = $form.data('lineas-iniciales') || [];   // jQuery ya lo convierte de JSON
    // Producto que viene del botón "Abastecer" de la lista de productos: se agrega en cuanto
    // se carguen los productos de un proveedor que lo tenga (una sola vez).
    let productoInicial = Number($form.data('producto-inicial')) || null;

    const $proveedor = $('#ProveedorId');
    const $marcas = $('#marcasProveedor');
    const $aviso = $('#avisoProveedor');
    const $buscador = $('#buscadorProducto');
    const $resultados = $('#resultadosProducto');
    const $lineas = $('#lineas');
    const $vacio = $('#lineasVacio');
    const $botones = $('.btn-registrar');
    const modal = document.getElementById('modalConfirmarAbastecimiento');

    const IMAGEN_POR_DEFECTO = '/images/default-product.svg';
    const MAX_RESULTADOS = 8;

    let productos = [];        // productos del proveedor elegido (vienen del servidor)
    let confirmado = false;    // true cuando el admin aceptó el modal de confirmación

    // Mismo formato que PriceFormatter en el servidor: "Bs. 1,234.56".
    function formatoBs(n) {
        return 'Bs. ' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // ---------------------------------------------------------------
    // PROVEEDOR
    // ---------------------------------------------------------------

    function mostrarMarcas() {
        $marcas.empty();
        const texto = $proveedor.find('option:selected').data('marcas');
        if (!$proveedor.val() || $proveedor.val() === '0') return;

        const nombres = texto ? String(texto).split('|') : [];
        if (nombres.length === 0) {
            $marcas.append($('<span class="cell-muted">').text('Este proveedor no tiene marcas asignadas.'));
            return;
        }
        $marcas.append($('<span class="cell-muted">').text('Marcas:'));
        const $lista = $('<div class="chip-lista">');
        nombres.forEach(function (n) { $lista.append($('<span class="chip">').text(n)); });
        $marcas.append($lista);
    }

    // Carga los productos del proveedor. "restaurar" = volver a armar las filas que devolvió el servidor.
    function cargarProductos(restaurar) {
        productos = [];
        ocultarResultados();
        mostrarMarcas();

        const id = $proveedor.val();
        if (!id || id === '0') {
            $buscador.prop('disabled', true).val('').attr('placeholder', 'Primero elige un proveedor');
            recalcular();
            return;
        }

        $buscador.prop('disabled', true).attr('placeholder', 'Cargando productos...');

        $.getJSON(urlProductos, { proveedorId: id })
            .done(function (data) {
                productos = data;
                if (productos.length === 0) {
                    $buscador.attr('placeholder', 'No hay productos de las marcas de este proveedor');
                } else {
                    $buscador.prop('disabled', false).attr('placeholder', 'Buscar producto de estas marcas...');
                }

                if (restaurar) {
                    lineasIniciales.forEach(function (l) {
                        const p = productos.find(function (x) { return x.id === l.productoId; });
                        if (p) agregarLinea(p, l.cantidad, l.costoUnitario, false);
                    });
                }

                if (productoInicial) {
                    const p = productos.find(function (x) { return x.id === productoInicial; });
                    if (p) {
                        agregarLinea(p, '', '', true);
                        productoInicial = null;
                    }
                }
                recalcular();
            })
            .fail(function () {
                $buscador.attr('placeholder', 'No se pudieron cargar los productos. Recarga la página.');
            });
    }

    $proveedor.on('change', function () {
        // Las filas son de las marcas del proveedor anterior: se quitan.
        if ($lineas.children('.linea').length > 0) {
            $lineas.empty();
            $aviso.text('Se quitaron los productos agregados porque cambiaste de proveedor.').prop('hidden', false);
        } else {
            $aviso.prop('hidden', true);
        }
        cargarProductos(false);
    });

    // ---------------------------------------------------------------
    // BUSCADOR DE PRODUCTOS
    // ---------------------------------------------------------------

    function filaDe(productoId) {
        return $lineas.children('.linea[data-producto-id="' + productoId + '"]');
    }

    function mostrarResultados() {
        const texto = $buscador.val().trim().toLowerCase();
        const coinciden = productos.filter(function (p) {
            return texto === '' ||
                p.nombre.toLowerCase().includes(texto) ||
                (p.marca || '').toLowerCase().includes(texto);
        }).slice(0, MAX_RESULTADOS);

        $resultados.empty();
        if (coinciden.length === 0) {
            $resultados.append($('<li class="buscador-resultados__vacio">').text('Sin coincidencias.'));
        }

        coinciden.forEach(function (p) {
            const yaAgregado = filaDe(p.id).length > 0;
            const $boton = $('<button type="button" class="buscador-resultados__item">').data('producto', p);
            $boton.append($('<img alt="" loading="lazy">').attr('src', p.imagenUrl || IMAGEN_POR_DEFECTO));
            const $texto = $('<span class="buscador-resultados__texto">');
            $texto.append($('<strong>').text(p.nombre));
            $texto.append($('<small>').text((p.marca || '') + ' · Stock ' + p.stock + (yaAgregado ? ' · ya agregado' : '')));
            $boton.append($texto);
            $resultados.append($('<li>').append($boton));
        });

        $resultados.prop('hidden', false);
    }

    function ocultarResultados() {
        $resultados.prop('hidden', true).empty();
    }

    $buscador.on('input focus', mostrarResultados);

    $buscador.on('keydown', function (e) {
        if (e.key === 'Escape') ocultarResultados();
        // Enter en el buscador agrega el primer resultado en vez de enviar el formulario.
        if (e.key === 'Enter') {
            e.preventDefault();
            $resultados.find('.buscador-resultados__item').first().trigger('click');
        }
    });

    $resultados.on('click', '.buscador-resultados__item', function () {
        agregarLinea($(this).data('producto'), '', '', true);
        $buscador.val('');
        ocultarResultados();
    });

    // Clic fuera del buscador: cerrar la lista.
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.buscador-producto').length) ocultarResultados();
    });

    // ---------------------------------------------------------------
    // FILAS (líneas del abastecimiento)
    // ---------------------------------------------------------------

    function agregarLinea(p, cantidad, costo, enfocar) {
        // El mismo producto no se agrega dos veces: se enfoca su fila.
        const $existente = filaDe(p.id);
        if ($existente.length) {
            $existente.find('.linea__cantidad').trigger('focus');
            $existente.removeClass('linea--resaltada');
            void $existente[0].offsetWidth;   // reinicia la animación
            $existente.addClass('linea--resaltada');
            return;
        }

        const $fila = $('<div class="linea linea--nueva">')
            .attr('data-producto-id', p.id)
            .attr('data-stock', p.stock);

        $fila.append($('<input type="hidden" class="linea__producto">').val(p.id));
        $fila.append($('<img class="linea__img" alt="" loading="lazy">').attr('src', p.imagenUrl || IMAGEN_POR_DEFECTO));

        const $info = $('<div class="linea__info">');
        $info.append($('<strong>').text(p.nombre));
        $info.append($('<small class="cell-muted">').text(p.marca || ''));
        $fila.append($info);

        const $stock = $('<div class="linea__stock">').attr('title', 'Stock actual → stock después de abastecer');
        $stock.append($('<span class="linea__etiqueta">').text('Stock'));
        $stock.append($('<span>').text(p.stock));
        $stock.append(' → ');
        $stock.append($('<strong class="linea__stock-nuevo">').text(p.stock));
        $fila.append($stock);

        const $cantidad = $('<label class="linea__campo">')
            .append($('<span class="linea__etiqueta">').text('Cantidad'))
            .append($('<input type="number" class="form-control linea__cantidad" min="1" max="10000" step="1" inputmode="numeric" required>').val(cantidad));
        $fila.append($cantidad);

        const $costo = $('<label class="linea__campo">')
            .append($('<span class="linea__etiqueta">').text('Costo unit. (Bs.)'))
            .append($('<input type="number" class="form-control linea__costo" min="0.01" max="1000000" step="0.01" inputmode="decimal" required>').val(costo));
        $fila.append($costo);

        $fila.append($('<div class="linea__subtotal">').text(formatoBs(0)));
        $fila.append($('<button type="button" class="linea__quitar" aria-label="Quitar producto">').text('✕'));

        $lineas.append($fila);
        $aviso.prop('hidden', true);
        reindexar();
        recalcular();

        if (enfocar) $fila.find('.linea__cantidad').trigger('focus');
    }

    $lineas.on('input', 'input', recalcular);

    $lineas.on('click', '.linea__quitar', function () {
        const $fila = $(this).closest('.linea');
        $fila.addClass('linea--saliendo');
        // Espera la animación de salida (si el usuario prefiere menos movimiento, es inmediata por CSS).
        setTimeout(function () {
            $fila.remove();
            reindexar();
            recalcular();
        }, 200);
    });

    // Los nombres deben ser consecutivos (Lineas[0], Lineas[1], ...): si falta un índice,
    // el model binding deja de leer las filas siguientes. Por eso se renumeran al quitar una.
    function reindexar() {
        $lineas.children('.linea').each(function (i) {
            $(this).find('.linea__producto').attr('name', 'Lineas[' + i + '].ProductoId');
            $(this).find('.linea__cantidad').attr('name', 'Lineas[' + i + '].Cantidad');
            $(this).find('.linea__costo').attr('name', 'Lineas[' + i + '].CostoUnitario');
        });
    }

    // ---------------------------------------------------------------
    // RESUMEN (vista previa)
    // ---------------------------------------------------------------

    function recalcular() {
        let unidades = 0;
        let total = 0;
        let todasValidas = true;
        const $filas = $lineas.children('.linea').not('.linea--saliendo');

        $filas.each(function () {
            const $fila = $(this);
            const $cant = $fila.find('.linea__cantidad');
            const $costo = $fila.find('.linea__costo');
            const cantidad = parseInt($cant.val(), 10);
            const costo = parseFloat($costo.val());

            const cantidadOk = Number.isInteger(cantidad) && cantidad >= 1 && cantidad <= 10000;
            const costoOk = !isNaN(costo) && costo >= 0.01 && costo <= 1000000;
            $cant.toggleClass('is-invalid', $cant.val() !== '' && !cantidadOk);
            $costo.toggleClass('is-invalid', $costo.val() !== '' && !costoOk);
            if (!cantidadOk || !costoOk) todasValidas = false;

            const subtotal = cantidadOk && costoOk ? cantidad * costo : 0;
            const stockNuevo = Number($fila.data('stock')) + (cantidadOk ? cantidad : 0);

            $fila.find('.linea__subtotal').text(formatoBs(subtotal));
            const $nuevo = $fila.find('.linea__stock-nuevo');
            if ($nuevo.text() !== String(stockNuevo)) {
                $nuevo.text(stockNuevo).removeClass('cambio');
                void $nuevo[0].offsetWidth;
                $nuevo.addClass('cambio');
            }

            if (cantidadOk) unidades += cantidad;
            total += subtotal;
        });

        $('[data-resumen="productos"]').text($filas.length);
        $('[data-resumen="unidades"]').text(unidades);
        $('[data-resumen="total"]').text(formatoBs(total));

        $vacio.prop('hidden', $filas.length > 0);
        $('.lineas-encabezado').toggleClass('visible', $filas.length > 0);

        const hayProveedor = $proveedor.val() && $proveedor.val() !== '0';
        const puedeRegistrar = hayProveedor && $filas.length > 0 && todasValidas;
        $botones.prop('disabled', !puedeRegistrar);
        return { puedeRegistrar: puedeRegistrar, productos: $filas.length, unidades: unidades };
    }

    // ---------------------------------------------------------------
    // ENVÍO CON CONFIRMACIÓN
    // ---------------------------------------------------------------

    $form.on('submit', function (e) {
        if (confirmado) return;   // ya confirmó en el modal: se envía normalmente
        e.preventDefault();

        const estado = recalcular();
        if (!estado.puedeRegistrar) return;

        $('#textoConfirmacion').text(
            'Se sumarán ' + estado.unidades + ' unidades al stock de ' + estado.productos +
            ' producto(s). ¿Confirmas?');
        bootstrap.Modal.getOrCreateInstance(modal).show();
    });

    $('#btnConfirmarAbastecimiento').on('click', function () {
        confirmado = true;
        $(this).prop('disabled', true).text('Registrando...');   // evita doble envío
        $botones.prop('disabled', true);
        $form[0].submit();
    });

    // ---------------------------------------------------------------
    // INICIO
    // ---------------------------------------------------------------
    // Si el proveedor viene preseleccionado (?proveedorId=5 o el form volvió con errores), cargar sus productos.
    cargarProductos(true);
});
