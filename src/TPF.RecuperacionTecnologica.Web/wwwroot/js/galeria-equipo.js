(function () {
    document.querySelectorAll('[data-galeria]').forEach(iniciar);

    function iniciar(galeria) {
        var principal = galeria.querySelector('[data-galeria-principal]');
        var leyenda = galeria.querySelector('[data-galeria-leyenda]');
        var miniaturas = galeria.querySelectorAll('[data-miniatura]');

        if (!principal || !leyenda) return;

        var fotos = miniaturas.length > 0
            ? Array.prototype.map.call(miniaturas, function (miniatura) {
                return { src: miniatura.dataset.src, alt: miniatura.dataset.alt, angulo: miniatura.dataset.angulo };
            })
            : [{ src: principal.getAttribute('src'), alt: principal.alt, angulo: principal.dataset.angulo }];

        var actual = 0;

        function texto(indice) {
            var leyendaFoto = 'Foto ' + (indice + 1) + ' de ' + fotos.length;

            if (fotos[indice].angulo) {
                leyendaFoto += ' · ' + fotos[indice].angulo;
            }

            return leyendaFoto;
        }

        function mostrar(indice) {
            actual = (indice + fotos.length) % fotos.length;

            principal.src = fotos[actual].src;
            principal.alt = fotos[actual].alt;
            leyenda.textContent = texto(actual);

            miniaturas.forEach(function (miniatura, posicion) {
                if (posicion === actual) {
                    miniatura.setAttribute('aria-current', 'true');
                } else {
                    miniatura.removeAttribute('aria-current');
                }
            });
        }

        miniaturas.forEach(function (miniatura, posicion) {
            miniatura.addEventListener('click', function (evento) {
                evento.preventDefault();
                mostrar(posicion);
            });
        });

        var ampliar = galeria.querySelector('[data-galeria-ampliar]');
        var dialogo = galeria.querySelector('[data-lightbox]');

        if (!ampliar || !dialogo || typeof dialogo.showModal !== 'function') return;

        var imagen = dialogo.querySelector('[data-lightbox-img]');
        var leyendaAmpliada = dialogo.querySelector('[data-lightbox-leyenda]');

        function pintarAmpliada() {
            imagen.src = fotos[actual].src;
            imagen.alt = fotos[actual].alt;
            leyendaAmpliada.textContent = texto(actual);
        }

        function mover(paso) {
            mostrar(actual + paso);
            pintarAmpliada();
        }

        ampliar.addEventListener('click', function () {
            pintarAmpliada();
            dialogo.showModal();
        });

        dialogo.querySelector('[data-lightbox-cerrar]').addEventListener('click', function () {
            dialogo.close();
        });

        dialogo.querySelector('[data-lightbox-anterior]').addEventListener('click', function () {
            mover(-1);
        });

        dialogo.querySelector('[data-lightbox-siguiente]').addEventListener('click', function () {
            mover(1);
        });

        dialogo.addEventListener('click', function (evento) {
            if (evento.target === dialogo) {
                dialogo.close();
            }
        });

        dialogo.addEventListener('keydown', function (evento) {
            if (fotos.length < 2) return;

            if (evento.key === 'ArrowLeft') {
                mover(-1);
            } else if (evento.key === 'ArrowRight') {
                mover(1);
            }
        });
    }
})();