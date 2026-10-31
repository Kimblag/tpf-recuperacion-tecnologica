(function () {
    iniciarCarga(document.querySelector('[data-carga-fotos]'));
    iniciarEliminacion(document.querySelector('[data-eliminar-dialogo]'));

    function iniciarCarga(contenedor) {
        if (!contenedor) return;

        var formulario = contenedor.closest('form');
        var entrada = contenedor.querySelector('[data-fotos-entrada]');
        var zona = contenedor.querySelector('[data-fotos-zona]');
        var lista = contenedor.querySelector('[data-fotos-lista]');
        var aviso = contenedor.querySelector('[data-fotos-aviso]');
        var plantilla = contenedor.querySelector('[data-fotos-plantilla]');
        var enviar = formulario.querySelector('[data-fotos-enviar]');
        var dictamen = formulario.querySelector('[data-dictamen]');
        var existentes = parseInt(contenedor.dataset.existentes, 10) || 0;
        var maximo = parseInt(contenedor.dataset.maxArchivos, 10);
        var maximoBytes = parseInt(contenedor.dataset.maxBytes, 10);
        var dictamenQueExigeFoto = formulario.dataset.requiereFotoCon;
        var tiposValidos = ['image/jpeg', 'image/png'];

        function mostrarAviso(mensajes) {
            aviso.textContent = mensajes.join(' ');
            aviso.hidden = mensajes.length === 0;
        }

        function formatoTamano(bytes) {
            return bytes >= 1048576
                ? (bytes / 1048576).toFixed(1).replace('.', ',') + ' MB'
                : Math.max(1, Math.round(bytes / 1024)) + ' KB';
        }

        function actuales() {
            return Array.prototype.slice.call(lista.children);
        }

        function sincronizar() {
            var transferencia = new DataTransfer();

            actuales().forEach(function (elemento) {
                transferencia.items.add(elemento._archivo);
            });

            entrada.files = transferencia.files;

            if (enviar && enviar.hasAttribute('data-fotos-enviar-con-fotos')) {
                enviar.disabled = transferencia.files.length === 0;
            }
        }

        function repetido(archivo) {
            return actuales().some(function (elemento) {
                var otro = elemento._archivo;

                return otro.name === archivo.name && otro.size === archivo.size && otro.lastModified === archivo.lastModified;
            });
        }

        function agregar(archivos) {
            var mensajes = [];

            Array.prototype.forEach.call(archivos, function (archivo) {
                if (repetido(archivo)) return;

                if (tiposValidos.indexOf(archivo.type) === -1) {
                    mensajes.push(archivo.name + ': solo se aceptan imágenes JPG o PNG.');
                    return;
                }

                if (archivo.size > maximoBytes) {
                    mensajes.push(archivo.name + ' supera el tamaño máximo de ' + formatoTamano(maximoBytes) + '.');
                    return;
                }

                if (actuales().length >= maximo) {
                    mensajes.push('Se pueden elegir hasta ' + maximo + (maximo === 1 ? ' foto' : ' fotos') + ' por envío. No se agregó ' + archivo.name + '.');
                    return;
                }

                lista.appendChild(crearItem(archivo));
            });

            mostrarAviso(mensajes);
            sincronizar();
        }

        function crearItem(archivo) {
            var elemento = plantilla.content.firstElementChild.cloneNode(true);
            var imagen = elemento.querySelector('img');
            var url = URL.createObjectURL(archivo);
            var selector = elemento.querySelector('[data-previa-angulo]');
            var etiqueta = elemento.querySelector('[data-previa-etiqueta]');
            var identificador = 'angulo-nuevo-' + Date.now() + '-' + lista.children.length;

            elemento._archivo = archivo;
            imagen.src = url;
            imagen.alt = 'Vista previa de ' + archivo.name;
            elemento.querySelector('[data-previa-nombre]').textContent = archivo.name;
            elemento.querySelector('[data-previa-tamano]').textContent = formatoTamano(archivo.size);
            selector.id = identificador;
            etiqueta.setAttribute('for', identificador);
            etiqueta.textContent = 'Ángulo de ' + archivo.name;

            elemento.querySelector('[data-previa-quitar]').addEventListener('click', function () {
                URL.revokeObjectURL(url);
                elemento.remove();
                mostrarAviso([]);
                sincronizar();
            });

            return elemento;
        }

        entrada.addEventListener('change', function () {
            agregar(entrada.files);
        });

        ['dragenter', 'dragover'].forEach(function (evento) {
            zona.addEventListener(evento, function (e) {
                e.preventDefault();
                zona.classList.add('dropzone-activa');
            });
        });

        ['dragleave', 'drop'].forEach(function (evento) {
            zona.addEventListener(evento, function (e) {
                e.preventDefault();
                zona.classList.remove('dropzone-activa');
            });
        });

        zona.addEventListener('drop', function (e) {
            agregar(e.dataTransfer.files);
        });

        formulario.addEventListener('submit', function (evento) {
            var exigeFoto = dictamen && dictamenQueExigeFoto !== undefined && dictamen.value === dictamenQueExigeFoto;

            if (exigeFoto && existentes + actuales().length === 0) {
                evento.preventDefault();
                mostrarAviso(['Para dejar el equipo disponible se necesita al menos una foto.']);
                entrada.focus();
                return;
            }

            if (enviar) {
                enviar.disabled = true;
                enviar.textContent = enviar.dataset.textoEnviando || 'Cargando…';
            }
        });
    }

    function iniciarEliminacion(dialogo) {
        if (!dialogo || typeof dialogo.showModal !== 'function') return;

        var formulario = dialogo.querySelector('form');
        var detalle = dialogo.querySelector('[data-eliminar-detalle]');

        document.querySelectorAll('[data-eliminar-foto]').forEach(function (boton) {
            boton.addEventListener('click', function () {
                formulario.action = boton.dataset.accion;
                detalle.textContent = boton.dataset.detalle.toLowerCase();
                dialogo.showModal();
            });
        });

        dialogo.querySelector('[data-eliminar-cancelar]').addEventListener('click', function () {
            dialogo.close();
        });

        dialogo.addEventListener('click', function (evento) {
            if (evento.target === dialogo) {
                dialogo.close();
            }
        });
    }
})();
