document.addEventListener('DOMContentLoaded', function () {
    const sidebarToggler = document.querySelector('[data-admin-toggle="sidebar"]');
    const sidebar = document.querySelector('.admin-sidebar');
    const mainContent = document.querySelector('.admin-main'); // Lấy thêm thẻ main

    if (sidebarToggler && sidebar && mainContent) {
        // 1. Tự động giữ trạng thái đóng/mở khi Admin chuyển trang
        if (localStorage.getItem('sidebarState') === 'collapsed') {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('main-expanded');
        }

        // 2. Lắng nghe sự kiện click nút ba gạch (bi-list)
        sidebarToggler.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('main-expanded'); // Co giãn khung main đồng thời

            // Lưu lại trạng thái vào localStorage để không bị reset khi chuyển trang
            if (sidebar.classList.contains('collapsed')) {
                localStorage.setItem('sidebarState', 'collapsed');
            } else {
                localStorage.setItem('sidebarState', 'expanded');
            }
        });
    }

    // Giữ nguyên bộ hiển thị Toast thông báo của Hào
    const toastEl = document.querySelector('.toast');
    if (toastEl) {
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    }
});

if (localStorage.getItem("darkMode") === "enabled") {
    document.body.classList.add("dark-theme");
    document.documentElement.style.colorScheme = "dark";
} else {
    document.documentElement.style.colorScheme = "light";
}

document.addEventListener("DOMContentLoaded", function () {
    const sidebarNav = document.getElementById("adminNavSidebar");
    if (sidebarNav) {
        const savedScrollTop = localStorage.getItem("shipperSidebarScrollPosition");
        if (savedScrollTop !== null) {
            sidebarNav.scrollTop = parseInt(savedScrollTop, 10);
        }
        sidebarNav.addEventListener("scroll", function () {
            localStorage.setItem("shipperSidebarScrollPosition", sidebarNav.scrollTop);
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const toggleBtn = document.getElementById("dark-mode-toggle");
    const modeIcon = document.getElementById("dark-mode-icon");
    const metaTheme = document.getElementById("browser-theme-color");
    const body = document.body;
    const html = document.documentElement;

    if (body.classList.contains("dark-theme")) {
        if (modeIcon) modeIcon.classList.replace("bi-moon", "bi-moon-fill");
        if (metaTheme) metaTheme.setAttribute("content", "#1e1e1e");
    }

    toggleBtn.addEventListener("click", function () {
        body.classList.add("theme-transitioning");
        body.classList.toggle("dark-theme");
        const currentIsDark = body.classList.contains("dark-theme");
        
        if (currentIsDark) {
            modeIcon.classList.replace("bi-moon", "bi-moon-fill");
            localStorage.setItem("darkMode", "enabled");
            html.style.colorScheme = "dark";
            if (metaTheme) metaTheme.setAttribute("content", "#1e1e1e");
        } else {
            modeIcon.classList.replace("bi-moon-fill", "bi-moon");
            localStorage.setItem("darkMode", "disabled");
            html.style.colorScheme = "light";
            if (metaTheme) metaTheme.setAttribute("content", "#ffffff");
        }

        setTimeout(() => {
            body.classList.remove("theme-transitioning");
        }, 1550);
    });

    // Script điều khiển đóng mở sidebar
    const toggleSidebarBtn = document.querySelector('[data-admin-toggle="sidebar"]');
    const sidebar = document.querySelector('.admin-sidebar');
    const mainContent = document.querySelector('.admin-main');
    if (toggleSidebarBtn && sidebar && mainContent) {
        toggleSidebarBtn.addEventListener("click", function () {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('main-expanded');
        });
    }
});

