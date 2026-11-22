(function () {
    var formulario = document.querySelector('[data-formulario-donacion]');
    if (!formulario) return;

    var resumen = document.getElementById('resumen-errores');
    if (resumen) resumen.focus();

    var tipo = formulario.querySelector('#TipoEquipo');
    var camposPorTipo = formulario.querySelectorAll('[data-tipos]');
    var etiquetaSerie = formulario.querySelector('[data-etiqueta-serie]');

    // valor de TipoEquipo.Celular: en los celulares el número que se pide es el IMEI
    var TIPO_CELULAR = '3';

    function mostrarCamposDelTipo() {
        if (etiquetaSerie) {
            etiquetaSerie.textContent = tipo.value === TIPO_CELULAR ? 'IMEI' : 'Número de serie';
        }

        camposPorTipo.forEach(function (campo) {
            var visible = campo.dataset.tipos.split(',').indexOf(tipo.value) !== -1;
            campo.hidden = !visible;
            campo.querySelectorAll('input, select, textarea').forEach(function (control) {
                control.disabled = !visible;
            });
        });
    }

    tipo.addEventListener('change', mostrarCamposDelTipo);
    mostrarCamposDelTipo();

    var boton = formulario.querySelector('button[type="submit"]');

    formulario.addEventListener('submit', function (evento) {
        setTimeout(function () {
            if (!evento.defaultPrevented) {
                boton.disabled = true;
            }
        }, 0);
    });

    function actualizarResumen() {
        var aviso = document.getElementById('resumen-errores');
        if (!aviso) return;

        var lista = aviso.querySelector('ul');
        if (!lista) return;

        lista.innerHTML = '';

        document.querySelectorAll('[data-valmsg-for]').forEach(function (mensaje) {
            if (!mensaje.textContent.trim()) return;

            var contenedor = mensaje.closest('.field') || mensaje.closest('form') || document;
            var etiqueta = contenedor.querySelector('label');
            var control = contenedor.querySelector('input:not([type="hidden"]), select, textarea');
            var item = document.createElement('li');
            var enlace = document.createElement('a');

            enlace.textContent = etiqueta ? etiqueta.firstChild.textContent.trim() : mensaje.getAttribute('data-valmsg-for');
            if (control && control.id) enlace.href = '#' + control.id;
            item.appendChild(enlace);
            lista.appendChild(item);
        });

        var titulo = aviso.querySelector('.alert-title');
        if (titulo) titulo.textContent = 'Revisá los campos marcados';

        aviso.hidden = false;
        setTimeout(function () { aviso.focus(); }, 0);
    }

    if (window.jQuery) {
        window.jQuery(formulario).on('invalid-form.validate', function () {
            if (formulario.hasAttribute('data-resumen-servidor')) {
                boton.disabled = true;
                formulario.submit();
                return;
            }

            setTimeout(actualizarResumen, 0);
        });
    }

    window.addEventListener('pageshow', function () {
        boton.disabled = false;
    });
})();