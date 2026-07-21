$(document).ready(function () {
    // 1. Tự động nạp danh sách thông báo thật vào chuông khi tải xong trang
    loadNotificationsToDropdown();

    // 2. Bắt sự kiện bấm vào thông báo để tải giao diện cshtml đổ vào Pop-up
    $(document).on('click', '.noti-item-trigger', function (e) {
        e.preventDefault();
        var orderId = $(this).data('order-id');
        
        // Mở Pop-up Modal lên và hiện hiệu ứng loading chờ Server phản hồi
        $('#orderTrackingModal').modal('show');
        $('#trackingModalBody').html('<div class="py-4 text-center"><div class="spinner-border text-success" role="status"></div><p class="text-muted small mt-2">Đang tải hành trình...</p></div>');

        // Gọi AJAX gửi ID sang Controller nhận về file cshtml đã biên dịch sẵn dữ liệu
        $.ajax({
            url: '/Notification/GetOrderTrackingPopup',
            type: 'GET',
            data: { orderId: orderId },
            success: function (htmlResult) {
                // Đổ trực tiếp toàn bộ mã HTML của file _OrderTrackingPopup.cshtml vào Modal
                $('#trackingModalBody').html(htmlResult);
            },
            error: function () {
                $('#trackingModalBody').html('<div class="alert alert-danger small m-0">Không thể kết nối máy chủ!</div>');
            }
        });
    });

    // 3. Hiển thị các thông báo SweetAlert2 từ TempData
    if (window.appNotifications) {
        if (window.appNotifications.newUserWelcome) {
            Swal.fire({
                title: '🎉 QUÀ RA MẮT THÀNH VIÊN MỚI! 🎉',
                html: window.appNotifications.newUserWelcome,
                icon: 'success',
                confirmButtonText: 'Tuyệt vời, mua sắm ngay!',
                confirmButtonColor: '#198754',
                allowOutsideClick: true
            });
        } else if (window.appNotifications.loginSuccessMessage) {
            Swal.fire({
                title: 'CHÀO MỪNG TRỞ LẠI!',
                html: window.appNotifications.loginSuccessMessage,
                icon: 'success',
                confirmButtonText: 'Bắt đầu mua sắm ngay!',
                confirmButtonColor: '#198754',
                allowOutsideClick: true
            });
        } else if (window.appNotifications.orderSuccessMessage) {
            Swal.fire({
                title: 'SỬ DỤNG ĐIỂM THÀNH CÔNG!',
                html: window.appNotifications.orderSuccessMessage,
                icon: 'success',
                confirmButtonColor: '#198754',
                allowOutsideClick: true
            });
        } else if (window.appNotifications.sendOTPSuccessMessage) {
            Swal.fire({
                title: 'ĐÃ GỬI MÃ OTP!',
                html: window.appNotifications.sendOTPSuccessMessage,
                icon: 'info',
                confirmButtonText: 'Hiểu rồi',
                confirmButtonColor: '#198754',
                allowOutsideClick: true
            });
        } else if (window.appNotifications.resetPasswordSuccessMessage) {
            Swal.fire({
                title: 'ĐỔI MẬT KHẨU THÀNH CÔNG!',
                html: window.appNotifications.resetPasswordSuccessMessage,
                icon: 'success',
                confirmButtonText: 'Đăng nhập ngay',
                confirmButtonColor: '#198754',
                allowOutsideClick: true
            });
        }
    }
});

// Hàm nạp thông báo thả xuống đa dạng loại
function loadNotificationsToDropdown() {
    $.ajax({
        url: '/Notification/GetNotificationsJson',
        type: 'GET',
        success: function (res) {
            if (res.success && res.data && res.data.length > 0) {
                var notiHtml = '';
                
                res.data.forEach(function (item) {
                    var triggerClass = item.isPopup ? 'noti-item-trigger' : '';
                    var targetUrl = item.isPopup ? '#' : item.url;

                    notiHtml += `
                        <a href="${targetUrl}" 
                           class="list-group-item list-group-item-action p-3 ${triggerClass} border-bottom" 
                           data-order-id="${item.id}">
                            <div class="d-flex w-100 justify-content-between align-items-center mb-1">
                                <strong class="small fw-bold text-dark">
                                    <i class="bi ${item.icon} me-1"></i> ${item.title}
                                </strong>
                                <small class="text-muted" style="font-size:0.72rem;">${item.time}</small>
                            </div>
                            <p class="mb-1 text-muted text-truncate" style="font-size:0.8rem; max-width:300px;">
                                ${item.content}
                            </p>
                            <div>
                                <span class="badge ${item.badgeClass} style="font-size:0.65rem;">
                                    ${getCategoryName(item.type)}
                                </span>
                            </div>
                        </a>
                    `;
                });

                $('#notiDropdownContainer').html(notiHtml);
                $('.bi-bell').siblings('.rounded-circle').show();
            } else {
                $('#notiDropdownContainer').html(`
                    <div class="p-4 text-center text-muted small">
                        <i class="bi bi-chat-left-dots fs-3 d-block mb-2 opacity-50"></i> Không có thông báo mới nào!
                    </div>
                `);
                $('.bi-bell').siblings('.rounded-circle').hide();
            }
        },
        error: function () {
            $('#notiDropdownContainer').html('<div class="p-3 text-center text-danger small">Không thể nạp dữ liệu thông báo!</div>');
        }
    });
}

// Helper lấy nhãn hiển thị cho loại thông báo
function getCategoryName(type) {
    switch (type) {
        case 'order': return 'Đơn hàng';
        case 'promo': return 'Ưu đãi';
        case 'support': return 'Khiếu nại/CSKH';
        case 'reward': return 'Tích điểm';
        default: return 'Hệ thống';
    }
}