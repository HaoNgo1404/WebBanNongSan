document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.toggle-password').forEach(function (button) {
        button.addEventListener('click', function () {
            var input = button.parentElement.querySelector('.password-field');
            if (!input) return;
            var icon = button.querySelector('i');
            var isHidden = input.getAttribute('type') === 'password';
            input.setAttribute('type', isHidden ? 'text' : 'password');
            icon.className = isHidden ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });
});
