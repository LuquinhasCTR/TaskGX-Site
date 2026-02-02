document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('modalNovaTarefa');
    const listaInput = document.getElementById('lista_id_tarefa');

    if (modal && listaInput) {
        modal.addEventListener('show.bs.modal', event => {
            const trigger = event.relatedTarget;
            const listaId = trigger?.getAttribute('data-lista-id') || '';
            listaInput.value = listaId;
        });
    }
});
