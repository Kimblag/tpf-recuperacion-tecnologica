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

    window.addEventListener('pageshow', function () {
        boton.disabled = false;
    });
})();