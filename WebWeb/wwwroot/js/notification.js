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
});

// Hàm lấy dữ liệu thật đổ vào danh sách chuông thông báo thả xuống
function loadNotificationsToDropdown() {
    $.ajax({
        url: '/Notification/GetNotificationsJson',
        type: 'GET',
        success: function (res) {
            if (res.success && res.data && res.data.length > 0) {
                var notiHtml = '';
                
                res.data.forEach(function (donHang) {
                    var bieuTuong = 'bi-box-seam';
                    var mauSac = 'text-info';
                    var tieuDe = 'Trạng thái đơn hàng #' + donHang.orderId;
                    var noiDung = 'Nhấn để xem lộ trình xử lý nông sản của đơn hàng này.';

                    if (donHang.trangThai === "ChoDuyet") {
                        bieuTuong = 'bi-file-earmark-text'; mauSac = 'text-info';
                        tieuDe = 'Đơn hàng #' + donHang.orderId + ' đang chờ duyệt';
                        noiDung = 'Hệ thống đã tiếp nhận đơn hàng của bạn và đang chờ xác nhận.';
                    } else if (donHang.trangThai === "ChoXuLy") {
                        bieuTuong = 'bi-box-seam-fill'; mauSac = 'text-primary';
                        tieuDe = 'Đơn hàng #' + donHang.orderId + ' đã được duyệt';
                        noiDung = 'Nhà vườn đang tiến hành thu hoạch nông sản tươi và đóng gói.';
                    } else if (donHang.trangThai === "DangGiao") {
                        bieuTuong = 'bi-truck'; mauSac = 'text-warning';
                        tieuDe = 'Đơn hàng #' + donHang.orderId + ' đang được giao';
                        noiDung = 'Đơn hàng nông sản đã được bàn giao cho shipper. Vui lòng chú ý máy.';
                    } else if (donHang.trangThai === "HoanThanh") {
                        bieuTuong = 'bi-check-circle-fill'; mauSac = 'text-success';
                        tieuDe = 'Đơn hàng #' + donHang.orderId + ' giao thành công 🎉';
                        noiDung = 'Cảm ơn bạn đã tin tưởng ủng hộ nông sản sạch GreenFresh.';
                    } else if (donHang.trangThai === "DaHuy") {
                        bieuTuong = 'bi-x-circle'; mauSac = 'text-secondary';
                        tieuDe = 'Đơn hàng #' + donHang.orderId + ' đã hủy';
                        noiDung = 'Đơn hàng nông sản này đã bị hủy bỏ trên hệ thống.';
                    }

                    notiHtml += `
                        <a href="#" class="list-group-item list-group-item-action p-3 noti-item-trigger border-bottom" data-order-id="${donHang.orderId}">
                            <div class="d-flex w-100 justify-content-between align-items-center mb-1">
                                <strong class="${mauSac} small fw-bold"><i class="bi ${bieuTuong} me-1"></i> ${tieuDe}</strong>
                                <small class="text-muted" style="font-size:0.75rem;">${donHang.ngayDat}</small>
                            </div>
                            <p class="mb-0 text-muted text-truncate" style="font-size:0.8rem; max-width:320px;">${noiDung}</p>
                        </a>
                    `;
                });

                $('#notiDropdownContainer').html(notiHtml);
                $('.bi-bell').siblings('.rounded-circle').show();
            } else {
                $('#notiDropdownContainer').html(`
                    <div class="p-4 text-center text-muted small">
                        <i class="bi bi-chat-left-dots fs-3 d-block mb-2 opacity-50"></i> Bạn chưa có thông báo tiến độ nào!
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