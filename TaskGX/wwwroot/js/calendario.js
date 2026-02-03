document.addEventListener("DOMContentLoaded", () => {
    const calendarEl = document.getElementById("calendar");
    if (!calendarEl) {
        return;
    }

    const filtroLista = document.getElementById("filtroLista");
    const filtroPrioridade = document.getElementById("filtroPrioridade");
    const mostrarConcluidas = document.getElementById("mostrarConcluidas");

    const tarefas = (window.calendarData && window.calendarData.tarefas) || [];

    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: "dayGridMonth",
        locale: "pt-br",
        height: "auto",
        headerToolbar: {
            left: "prev,next today",
            center: "title",
            right: "dayGridMonth,timeGridWeek,timeGridDay"
        }
    });

    const getFilteredEvents = () => {
        const listaId = filtroLista && filtroLista.value ? Number(filtroLista.value) : null;
        const prioridadeId = filtroPrioridade && filtroPrioridade.value ? Number(filtroPrioridade.value) : null;
        const incluiConcluidas = !mostrarConcluidas || mostrarConcluidas.checked;

        return tarefas
            .filter((tarefa) => {
                if (!incluiConcluidas && tarefa.concluida) {
                    return false;
                }

                if (listaId && tarefa.listaId !== listaId) {
                    return false;
                }

                if (prioridadeId && tarefa.prioridadeId !== prioridadeId) {
                    return false;
                }

                return true;
            })
            .map((tarefa) => {
                const data = tarefa.dataVencimento || tarefa.dataCriacao;
                return {
                    id: tarefa.id,
                    title: tarefa.titulo,
                    start: data,
                    allDay: true,
                    extendedProps: tarefa
                };
            })
            .filter((evento) => Boolean(evento.start));
    };

    const refreshEvents = () => {
        calendar.removeAllEvents();
        calendar.addEventSource(getFilteredEvents());
    };

    calendar.render();
    refreshEvents();

    if (filtroLista) {
        filtroLista.addEventListener("change", refreshEvents);
    }

    if (filtroPrioridade) {
        filtroPrioridade.addEventListener("change", refreshEvents);
    }

    if (mostrarConcluidas) {
        mostrarConcluidas.addEventListener("change", refreshEvents);
    }
});
