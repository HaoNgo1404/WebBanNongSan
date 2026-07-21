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

document.addEventListener("DOMContentLoaded", function () {
    const inputSearch = document.getElementById('txtSearchHeader');
    const suggestionWrapper = document.getElementById('searchSuggestionWrapper');

    if (inputSearch && suggestionWrapper) {

        // Hàm chung để load danh sách gợi ý từ Server
        function fetchSuggestions(text = '') {
            fetch(`/Search/Suggest?keyword=${encodeURIComponent(text)}`)
                .then(response => response.text())
                .then(html => {
                    if (html.trim() !== "") {
                        suggestionWrapper.innerHTML = html;
                        suggestionWrapper.classList.remove('d-none');
                    } else {
                        suggestionWrapper.classList.add('d-none');
                    }
                })
                .catch(err => console.error("Lỗi load gợi ý:", err));
        }

        // 1. Khi người dùng click/focus vào ô search: Load danh sách ngay lập tức
        inputSearch.addEventListener('focus', function () {
            let text = this.value.trim();
            fetchSuggestions(text); // Load danh sách (có thể là mặc định nếu text rỗng)
        });

        // 2. Lắng nghe khi người dùng gõ phím
        inputSearch.addEventListener('input', function () {
            let text = this.value.trim();
            fetchSuggestions(text); // Load theo từ khóa đang gõ
        });

        // 3. Ẩn hộp gợi ý khi click ra ngoài vùng tìm kiếm
        document.addEventListener('click', function (e) {
            if (!inputSearch.contains(e.target) && !suggestionWrapper.contains(e.target)) {
                suggestionWrapper.classList.add('d-none');
            }
        });
    }
});

// Hàm xử lý ẩn/hiện mật khẩu viết độc lập
function togglePasswordVisibility(button) {
    // Tìm ô input nằm cùng cấp lớp hoặc gần nhất với nút bấm này
    const inputGroup = button.closest('.input-group');
    if (!inputGroup) return;

    const passwordField = inputGroup.querySelector('.password-field');
    const icon = button.querySelector('i');

    if (passwordField && icon) {
        // Chuyển đổi thuộc tính type
        if (passwordField.type === 'password') {
            passwordField.type = 'text';
            icon.className = 'bi bi-eye-slash'; // Đổi sang mắt gạch chéo
        } else {
            passwordField.type = 'password';
            icon.className = 'bi bi-eye';       // Đổi về mắt bình thường
        }
    }
}

function quickAddToCart(productId) {
    const formData = new FormData();
    formData.append("id", productId);
    formData.append("quantity", 1);

    fetch('/Cart/Add', {
        method: 'POST',
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (response.ok) {
            alert("Đã thêm sản phẩm vào giỏ hàng!");
            window.location.reload(); // Hoặc cập nhật badge giỏ hàng nếu bạn có JS riêng
        } else {
            alert("Không thể thêm vào giỏ hàng. Vui lòng kiểm tra lại!");
        }
    })
    .catch(err => {
        console.error("Lỗi khi thêm giỏ hàng:", err);
    });
}