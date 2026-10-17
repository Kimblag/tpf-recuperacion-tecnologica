(function () {
    var modal = document.getElementById('modal-cancelar-lista');

    if (!modal) return;

    modal.addEventListener('show.bs.modal', function (evento) {
        var origen = evento.relatedTarget;

        if (!origen) return;

        modal.querySelector('input[name="NroEquipo"]').value = origen.dataset.nro;
        modal.querySelector('[data-cancelar-codigo]').textContent = origen.dataset.codigo;
        modal.querySelector('[data-cancelar-equipo]').textContent = origen.dataset.equipo;
    });
})();
