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
