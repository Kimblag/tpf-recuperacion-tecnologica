(function () {
    var anchos = [70, 55, 85, 60, 45, 65, 50];

    document.querySelectorAll('[data-listado]').forEach(function (listado) {
        var cuerpo = listado.querySelector('.card-body');
        var plantillaNota = listado.querySelector('template[data-plantilla-carga]');

        if (!cuerpo || !plantillaNota) return;

        var filas = parseInt(listado.dataset.filasCarga, 10) || 6;
        var original = null;
        var temporizador = null;

        function construirTabla() {
            var etiquetas = Array.from(cuerpo.querySelectorAll('thead th')).map(function (celda) {
                return celda.textContent.trim();
            });
            var columnas = etiquetas.length || 5;

            var envoltorio = document.createElement('div');
            envoltorio.className = 'table-wrap';

            var tabla = document.createElement('table');
            tabla.className = 'table';

            if (etiquetas.length > 0) {
                var encabezado = tabla.createTHead().insertRow();

                etiquetas.forEach(function (etiqueta) {
                    var celda = document.createElement('th');
                    celda.scope = 'col';
                    celda.textContent = etiqueta;
                    encabezado.appendChild(celda);
                });
            }

            var cuerpoTabla = tabla.createTBody();

            for (var fila = 0; fila < filas; fila++) {
                var filaNueva = cuerpoTabla.insertRow();

                for (var columna = 0; columna < columnas; columna++) {
                    var barra = document.createElement('span');
                    barra.className = 'skel';
                    barra.style.width = anchos[columna % anchos.length] + '%';
                    filaNueva.insertCell().appendChild(barra);
                }
            }

            envoltorio.appendChild(tabla);
            return envoltorio;
        }

        function mostrarCarga() {
            temporizador = null;

            if (original !== null) return;

            original = cuerpo.innerHTML;
            cuerpo.replaceChildren(construirTabla(), plantillaNota.content.cloneNode(true));
            listado.setAttribute('aria-busy', 'true');
        }

        function programarCarga() {
            if (temporizador === null && original === null) {
                temporizador = setTimeout(mostrarCarga, 150);
            }
        }

        listado.addEventListener('submit', function (evento) {
            if (!evento.defaultPrevented) programarCarga();
        });

        listado.addEventListener('click', function (evento) {
            if (evento.button !== 0 || evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) return;

            var enlace = evento.target.closest('.orden-link, a.page-btn, [data-muestra-carga]');

            if (enlace && listado.contains(enlace)) programarCarga();
        });

        window.addEventListener('pageshow', function (evento) {
            clearTimeout(temporizador);
            temporizador = null;

            if (evento.persisted && original !== null) {
                cuerpo.innerHTML = original;
                original = null;
                listado.removeAttribute('aria-busy');
            }
        });
    });
})();
