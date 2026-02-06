document.addEventListener('DOMContentLoaded', function () {
    // =========================
    // Toggle mostrar/ocultar senha
    // =========================
    const toggleSenha = document.getElementById('toggleSenha');
    const toggleConfirmarSenha = document.getElementById('toggleConfirmarSenha');

    function toggle(inputId, iconId) {
        const input = document.getElementById(inputId);
        const icon = document.getElementById(iconId);
        if (!input) return;
        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.remove('bi-eye');
            icon.classList.add('bi-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.remove('bi-eye-slash');
            icon.classList.add('bi-eye');
        }
    }

    toggleSenha?.addEventListener('click', function () {
        toggle('senha', 'iconSenha');
    });

    toggleConfirmarSenha?.addEventListener('click', function () {
        toggle('confirmar_senha', 'iconConfirmarSenha');
    });

    // =========================
    // Checagem de igualdade de senha
    // =========================
    const senhaInput = document.getElementById('senha');
    const confirmarInput = document.getElementById('confirmar_senha');
    const senhaMatchText = document.getElementById('senhaMatch');

    function checkMatch() {
        if (!senhaInput || !confirmarInput || !senhaMatchText) return;
        if (confirmarInput.value.length === 0) {
            senhaMatchText.textContent = '';
            return;
        }
        if (senhaInput.value === confirmarInput.value) {
            senhaMatchText.textContent = 'Senhas iguais';
            senhaMatchText.classList.remove('text-danger');
            senhaMatchText.classList.add('text-success');
        } else {
            senhaMatchText.textContent = 'As senhas não coincidem';
            senhaMatchText.classList.remove('text-success');
            senhaMatchText.classList.add('text-danger');
        }
    }

    senhaInput?.addEventListener('input', checkMatch);
    confirmarInput?.addEventListener('input', checkMatch);

    // =========================
    // Bootstrap validation + submit
    // =========================
    const form = document.querySelector('.needs-validation');
    if (!form) return;

    form.addEventListener('submit', function (event) {
        // Checa validade dos campos
        if (!form.checkValidity()) {
            event.preventDefault(); // previne submit se inválido
            event.stopPropagation();
        }
        form.classList.add('was-validated');
    }, false);
});
