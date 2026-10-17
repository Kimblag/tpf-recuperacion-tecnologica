(function () {
    document.querySelectorAll('[data-filtros-automaticos]').forEach(function (formulario) {
        formulario.querySelectorAll('select').forEach(function (selector) {
            selector.addEventListener('change', function () {
                formulario.requestSubmit();
            });
        });

        var texto = formulario.querySelector('[data-filtro-texto]');

        if (!texto) return;

        var teniaTexto = texto.value.trim() !== '';

        texto.addEventListener('input', function () {
            var hayTexto = texto.value.trim() !== '';

            if (teniaTexto && !hayTexto) {
                formulario.requestSubmit();
            }

            teniaTexto = hayTexto;
        });
    });
})();
