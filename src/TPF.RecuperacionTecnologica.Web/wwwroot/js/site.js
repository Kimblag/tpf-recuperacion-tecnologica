// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (elemento) {
    new bootstrap.Tooltip(elemento);
});

(function () {
    var prefijo = 'lista:';

    try {
        var guardable = document.querySelector('[data-lista-guardar]');

        if (guardable) {
            sessionStorage.setItem(prefijo + guardable.dataset.listaGuardar, location.search);
        }

        document.querySelectorAll('[data-volver-a]').forEach(function (enlace) {
            var guardada = sessionStorage.getItem(prefijo + enlace.dataset.volverA);

            if (guardada) {
                enlace.search = guardada;
            }
        });
    } catch (error) {
    }
})();

(function () {
    var indicador = document.getElementById('indicador-carga');

    if (!indicador) return;

    var temporizador = null;
    var cierre = null;

    function mostrar() {
        indicador.hidden = false;
        cierre = setTimeout(ocultar, 10000);
    }

    function ocultar() {
        clearTimeout(temporizador);
        clearTimeout(cierre);
        temporizador = null;
        indicador.hidden = true;
    }

    function programar() {
        if (temporizador === null) {
            temporizador = setTimeout(mostrar, 300);
        }
    }

    document.addEventListener('click', function (evento) {
        if (evento.button !== 0 || evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) return;

        var enlace = evento.target.closest('a[href]');

        if (!enlace || enlace.target || enlace.hasAttribute('download') || enlace.hasAttribute('data-bs-toggle')) return;
        if (enlace.origin !== location.origin) return;
        if (enlace.pathname === location.pathname && enlace.search === location.search && enlace.hash) return;

        programar();
    });

    document.addEventListener('submit', function (evento) {
        if (!evento.defaultPrevented) programar();
    });

    window.addEventListener('pageshow', ocultar);
})();
