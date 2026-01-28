(function () {
    'use strict';

    // Tempo em milissegundos antes de fechar (5 segundos)
    const AUTO_DISMISS_TIME = 5000;

    // Função para fechar um alert
    function dismissAlert(alertElement) {
        // Se o alert tem a classe dismissible, usar o método Bootstrap
        if (alertElement.classList.contains('alert-dismissible') && typeof bootstrap !== 'undefined') {
            try {
                const bsAlert = new bootstrap.Alert(alertElement);
                bsAlert.close();
            } catch (e) {
                // Fallback se Bootstrap não estiver disponível
                alertElement.style.transition = 'opacity 0.3s ease-out';
                alertElement.style.opacity = '0';
                setTimeout(function () {
                    alertElement.remove();
                }, 300);
            }
        } else {
            // Se não tem, apenas remover o elemento
            alertElement.style.transition = 'opacity 0.3s ease-out';
            alertElement.style.opacity = '0';
            setTimeout(function () {
                alertElement.remove();
            }, 300);
        }
    }

    // Função para inicializar o auto-dismiss
    function initAutoDismiss() {
        // Aguardar o DOM estar pronto
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function () {
                setupAlerts();
            });
        } else {
            setupAlerts();
        }
    }

    function setupAlerts() {
        // Buscar todos os alerts na página
        const alerts = document.querySelectorAll('.alert');

        alerts.forEach(function (alert) {
            // Ignorar alerts que já foram fechados ou que têm a classe 'no-auto-dismiss'
            if (alert.classList.contains('no-auto-dismiss') || alert.classList.contains('alert-dismissed')) {
                return;
            }

            // Marcar como processado
            alert.classList.add('alert-dismissed');

            // Configurar timeout para fechar automaticamente
            const timeoutId = setTimeout(function () {
                dismissAlert(alert);
            }, AUTO_DISMISS_TIME);

            // Se o usuário clicar no botão de fechar manualmente, cancelar o timeout
            const closeButton = alert.querySelector('.btn-close');
            if (closeButton) {
                closeButton.addEventListener('click', function () {
                    clearTimeout(timeoutId);
                });
            }

            // Pausar o auto-dismiss quando o mouse estiver sobre o alert
            alert.addEventListener('mouseenter', function () {
                clearTimeout(timeoutId);
            });

            // Retomar o auto-dismiss quando o mouse sair do alert
            alert.addEventListener('mouseleave', function () {
                const newTimeoutId = setTimeout(function () {
                    dismissAlert(alert);
                }, AUTO_DISMISS_TIME);
                // Armazenar o novo timeout para poder cancelá-lo se necessário
                alert.dataset.timeoutId = newTimeoutId;
            });
        });
    }

    // Inicializar quando o script for carregado
    initAutoDismiss();

    // Também observar novos alerts adicionados dinamicamente
    if (typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1 && node.classList && node.classList.contains('alert')) {
                        // Pequeno delay para garantir que o alert foi totalmente renderizado
                        setTimeout(setupAlerts, 100);
                    }
                });
            });
        });

        // Observar mudanças no body
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }
})();

