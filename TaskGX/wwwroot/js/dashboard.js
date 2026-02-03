document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('modalNovaTarefa');
    const listaInput = document.getElementById('lista_id_tarefa');
    const acaoInput = document.getElementById('acaoTarefa');
    const tarefaIdInput = document.getElementById('tarefa_id');
    const tituloModal = document.getElementById('tituloModalTarefa');
    const btnSalvar = document.getElementById('btnSalvarTarefa');
    const tituloInput = document.getElementById('tituloTarefa');
    const descricaoInput = document.getElementById('descricaoTarefa');
    const prioridadeInput = document.getElementById('prioridadeTarefa');
    const dataInput = document.getElementById('dataVencimento');
    const tagsInput = document.getElementById('tagsTarefa');

    if (modal && listaInput) {
        modal.addEventListener('show.bs.modal', event => {
            const trigger = event.relatedTarget;
            const listaId = trigger?.getAttribute('data-lista-id') || '';
            if (listaId) {
                listaInput.value = listaId;
            }
        });

        modal.addEventListener('hidden.bs.modal', () => {
            acaoInput.value = 'criar';
            tarefaIdInput.value = '';
            if (tituloModal) {
                tituloModal.textContent = 'Nova Tarefa';
            }
            if (btnSalvar) {
                btnSalvar.textContent = 'Criar Tarefa';
            }
            if (tituloInput) {
                tituloInput.value = '';
            }
            if (descricaoInput) {
                descricaoInput.value = '';
            }
            if (prioridadeInput) {
                prioridadeInput.value = '';
            }
            if (dataInput) {
                dataInput.value = '';
            }
            if (tagsInput) {
                tagsInput.value = '';
            }
        });
    }

    const buscaInput = document.getElementById('buscaTarefas');
    const statusSelect = document.getElementById('filtroStatus');
    const prioridadeSelect = document.getElementById('filtroPrioridade');

    const aplicarFiltros = () => {
        const termo = buscaInput?.value?.trim().toLowerCase() ?? '';
        const status = statusSelect?.value ?? 'todas';
        const prioridade = prioridadeSelect?.value ?? 'todas';

        document.querySelectorAll('.tarefa-item').forEach(item => {
            const titulo = item.getAttribute('data-titulo') || '';
            const descricao = item.getAttribute('data-descricao') || '';
            const itemStatus = item.getAttribute('data-status') || '';
            const itemPrioridade = item.getAttribute('data-prioridade') || '';

            const matchTexto = !termo || titulo.includes(termo) || descricao.includes(termo);
            const statusFiltro = status === 'pendentes' ? 'pendente' : status === 'concluidas' ? 'concluida' : status;
            const matchStatus = statusFiltro === 'todas' || itemStatus === statusFiltro;
            const matchPrioridade = prioridade === 'todas' || itemPrioridade === prioridade;

            item.style.display = matchTexto && matchStatus && matchPrioridade ? '' : 'none';
        });
    };

    buscaInput?.addEventListener('input', aplicarFiltros);
    statusSelect?.addEventListener('change', aplicarFiltros);
    prioridadeSelect?.addEventListener('change', aplicarFiltros);
});

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

async function postJson(url, payload) {
    const token = getAntiForgeryToken();
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        throw new Error('Falha na requisição');
    }

    return response.json();
}

function marcarTarefa(tarefaId, concluida) {
    postJson('/Tarefas/MarcarConcluida', { tarefaId, concluida })
        .then(result => {
            if (result?.success) {
                window.location.reload();
                return;
            }
            alert(result?.message || 'Erro ao marcar tarefa.');
        })
        .catch(() => alert('Erro ao marcar tarefa.'));
}


function excluirTarefa(tarefaId, titulo) {
    if (!confirm(`Deseja excluir a tarefa "${titulo}"?`)) {
        return;
    }

    postJson('/Tarefas/Excluir', { tarefaId })
        .then(result => {
            if (result?.success) {
                window.location.reload();
                return;
            }
            alert(result?.message || 'Erro ao excluir tarefa.');
        })
        .catch(() => alert('Erro ao excluir tarefa.'));
}

function duplicarTarefa(tarefaId) {
    postJson('/Tarefas/Duplicar', { tarefaId })
        .then(result => {
            if (result?.success) {
                window.location.reload();
                return;
            }
            alert(result?.message || 'Erro ao duplicar tarefa.');
        })
        .catch(() => alert('Erro ao duplicar tarefa.'));
}

function editarTarefa(tarefaId, titulo, descricao, prioridadeId, dataVencimento, tags) {
    const modalElement = document.getElementById('modalNovaTarefa');
    const modal = modalElement ? bootstrap.Modal.getOrCreateInstance(modalElement) : null;

    const acaoInput = document.getElementById('acaoTarefa');
    const tarefaIdInput = document.getElementById('tarefa_id');
    const tituloModal = document.getElementById('tituloModalTarefa');
    const btnSalvar = document.getElementById('btnSalvarTarefa');

    if (acaoInput) {
        acaoInput.value = 'editar';
    }
    if (tarefaIdInput) {
        tarefaIdInput.value = tarefaId;
    }
    if (tituloModal) {
        tituloModal.textContent = 'Editar Tarefa';
    }
    if (btnSalvar) {
        btnSalvar.textContent = 'Salvar Alterações';
    }

    const tituloInput = document.getElementById('tituloTarefa');
    const descricaoInput = document.getElementById('descricaoTarefa');
    const prioridadeInput = document.getElementById('prioridadeTarefa');
    const dataInput = document.getElementById('dataVencimento');
    const tagsInput = document.getElementById('tagsTarefa');

    if (tituloInput) {
        tituloInput.value = titulo || '';
    }
    if (descricaoInput) {
        descricaoInput.value = descricao || '';
    }
    if (prioridadeInput) {
        prioridadeInput.value = prioridadeId ?? '';
    }
    if (dataInput) {
        dataInput.value = dataVencimento || '';
    }
    if (tagsInput) {
        tagsInput.value = tags || '';
    }

    if (modal) {
        modal.show();
    }
}
