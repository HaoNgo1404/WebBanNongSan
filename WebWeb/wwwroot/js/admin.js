// 1. CHẠY NGAY (Không cần đợi DOM) - Tránh bị giật giao diện (Flicker) khi tải trang
if (localStorage.getItem("darkMode") === "enabled") {
    document.body.classList.add("dark-theme");
    document.documentElement.style.colorScheme = "dark";
} else {
    document.documentElement.style.colorScheme = "light";
}

// 2. CHẠY KHI DOM ĐÃ SẴN SÀNG
document.addEventListener('DOMContentLoaded', function () {
    
    // --- KHỐI XỬ LÝ SIDEBAR (CHỈ ĐĂNG KÝ 1 LẦN DUY NHẤT) ---
    const sidebarToggler = document.querySelector('[data-admin-toggle="sidebar"]');
    const sidebar = document.querySelector('.admin-sidebar');
    const mainContent = document.querySelector('.admin-main'); 

    if (sidebarToggler && sidebar && mainContent) {
        // Tự động khôi phục trạng thái đóng/mở từ trang trước
        if (localStorage.getItem('sidebarState') === 'collapsed') {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('main-expanded');
        }

        // Lắng nghe sự kiện click (Rút gọn duy nhất một chỗ)
        sidebarToggler.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('main-expanded'); 

            // Lưu lại trạng thái vào localStorage
            if (sidebar.classList.contains('collapsed')) {
                localStorage.setItem('sidebarState', 'collapsed');
            } else {
                localStorage.setItem('sidebarState', 'expanded');
            }
        });
    }

    // --- KHỐI GIỮ VỊ TRÍ CUỘN MENU SIDEBAR ---
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

    // --- KHỐI XỬ LÝ DARK MODE ---
    const toggleBtn = document.getElementById("dark-mode-toggle");
    const modeIcon = document.getElementById("dark-mode-icon");
    const metaTheme = document.getElementById("browser-theme-color");
    const body = document.body;
    const html = document.documentElement;

    if (body.classList.contains("dark-theme")) {
        if (modeIcon) modeIcon.classList.replace("bi-moon", "bi-moon-fill");
        if (metaTheme) metaTheme.setAttribute("content", "#1e1e1e");
    }

    if (toggleBtn) {
        toggleBtn.addEventListener("click", function () {
            body.classList.add("theme-transitioning");
            body.classList.toggle("dark-theme");
            const currentIsDark = body.classList.contains("dark-theme");
            
            if (currentIsDark) {
                if (modeIcon) modeIcon.classList.replace("bi-moon", "bi-moon-fill");
                localStorage.setItem("darkMode", "enabled");
                html.style.colorScheme = "dark";
                if (metaTheme) metaTheme.setAttribute("content", "#1e1e1e");
            } else {
                if (modeIcon) modeIcon.classList.replace("bi-moon-fill", "bi-moon");
                localStorage.setItem("darkMode", "disabled");
                html.style.colorScheme = "light";
                if (metaTheme) metaTheme.setAttribute("content", "#ffffff");
            }

            setTimeout(() => {
                body.classList.remove("theme-transitioning");
            }, 1550);
        });
    }

    // --- TOAST THÔNG BÁO ---
    const toastEl = document.querySelector('.toast');
    if (toastEl) {
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    }
});