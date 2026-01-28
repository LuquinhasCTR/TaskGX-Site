// Sistema de gerenciamento de tema
// Nota: O tema já foi aplicado no <head> para evitar flash branco
// Este script apenas gerencia o toggle e atualiza o ícone
(function () {
    'use strict';

    // Aguarda o DOM estar pronto
    function initTheme() {
        const themeToggle = document.getElementById('themeToggle');
        const themeIcon = document.getElementById('themeIcon');
        const html = document.documentElement;

        // Função para aplicar o tema
        function applyTheme(theme) {
            html.setAttribute('data-theme', theme);
            localStorage.setItem('theme', theme);

            // Força atualização de todos os elementos
            document.body.setAttribute('data-theme', theme);

            // Atualiza o ícone
            if (themeIcon) {
                if (theme === 'dark') {
                    themeIcon.classList.remove('bi-sun-fill');
                    themeIcon.classList.add('bi-moon-fill');
                } else {
                    themeIcon.classList.remove('bi-moon-fill');
                    themeIcon.classList.add('bi-sun-fill');
                }
            }

            // Dispara evento customizado para elementos que precisam atualizar
            window.dispatchEvent(new CustomEvent('themechange', { detail: { theme } }));
        }

        // Função para alternar o tema
        function toggleTheme() {
            const currentTheme = html.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(newTheme);
        }

        // Atualiza o ícone baseado no tema atual
        function updateIcon() {
            const currentTheme = html.getAttribute('data-theme') || 'light';
            if (themeIcon) {
                if (currentTheme === 'dark') {
                    themeIcon.classList.remove('bi-sun-fill');
                    themeIcon.classList.add('bi-moon-fill');
                } else {
                    themeIcon.classList.remove('bi-moon-fill');
                    themeIcon.classList.add('bi-sun-fill');
                }
            }
        }

        // Atualiza o ícone ao carregar
        updateIcon();

        // Adiciona o evento de clique no botão
        if (themeToggle) {
            themeToggle.addEventListener('click', toggleTheme);
        }
    }

    // Inicializa quando o DOM estiver pronto
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }
})();

