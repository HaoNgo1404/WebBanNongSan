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
        // Lắng nghe sự kiện gõ phím của khách hàng
        inputSearch.addEventListener('input', function () {
            let text = this.value.trim();
            
            if (text.length < 1) {
                suggestionWrapper.innerHTML = '';
                suggestionWrapper.classList.add('d-none');
                return;
            }

            // Gọi Fetch API lấy nội dung Partial gợi ý sản phẩm
            fetch(`/Search/Suggest?keyword=${encodeURIComponent(text)}`)
                .then(response => response.text())
                .then(html => {
                    if (html.trim() !== "") {
                        suggestionWrapper.innerHTML = html;
                        suggestionWrapper.classList.remove('d-none');
                    } else {
                        suggestionWrapper.classList.add('d-none');
                    }
                });
        });

        // Ẩn hộp gợi ý khi click ra ngoài vùng tìm kiếm
        document.addEventListener('click', function (e) {
            if (!inputSearch.contains(e.target) && !suggestionWrapper.contains(e.target)) {
                suggestionWrapper.classList.add('d-none');
            }
        });
        
        // Hiện lại nếu người dùng click tập trung vào ô input
        inputSearch.addEventListener('focus', function () {
            if (this.value.trim().length >= 1) {
                suggestionWrapper.classList.remove('d-none');
            }
        });
    }
});