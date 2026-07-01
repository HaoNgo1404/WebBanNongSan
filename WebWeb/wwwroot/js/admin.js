document.addEventListener('DOMContentLoaded', function () {
    const sidebarToggler = document.querySelector('[data-admin-toggle="sidebar"]');
    const sidebar = document.querySelector('.admin-sidebar');

    if (sidebarToggler && sidebar) {
        sidebarToggler.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
        });
    }

    const toastEl = document.querySelector('.toast');
    if (toastEl) {
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    }
});
