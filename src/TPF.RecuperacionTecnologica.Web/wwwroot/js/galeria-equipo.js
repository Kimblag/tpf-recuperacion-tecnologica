(function () {
    var galeria = document.querySelector('[data-galeria]');

    if (!galeria) return;

    var principal = galeria.querySelector('[data-galeria-principal]');
    var leyenda = galeria.querySelector('[data-galeria-leyenda]');
    var miniaturas = galeria.querySelectorAll('[data-miniatura]');

    if (!principal || !leyenda) return;

    miniaturas.forEach(function (miniatura) {
        miniatura.addEventListener('click', function (evento) {
            evento.preventDefault();

            principal.src = miniatura.dataset.src;
            principal.alt = miniatura.dataset.alt;

            miniaturas.forEach(function (otra) {
                if (otra === miniatura) {
                    otra.setAttribute('aria-current', 'true');
                } else {
                    otra.removeAttribute('aria-current');
                }
            });

            var texto = 'Foto ' + miniatura.dataset.numero + ' de ' + miniaturas.length;

            if (miniatura.dataset.angulo) {
                texto += ' · ' + miniatura.dataset.angulo;
            }

            leyenda.textContent = texto;
        });
    });
})();
