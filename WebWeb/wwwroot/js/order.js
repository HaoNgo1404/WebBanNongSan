document.addEventListener('DOMContentLoaded', function () {
    const stars = document.querySelectorAll('.review-star');

    if (!stars.length) {
        return;
    }

    stars.forEach((star, index) => {
        star.addEventListener('click', function () {
            stars.forEach((item, itemIndex) => {
                item.style.color = itemIndex <= index ? '#f59e0b' : '#d1d5db';
            });
        });
    });
});

// ========================================================
// LOGIC KHỞI TẠO BẢN ĐỒ CHO KHÁCH VÃNG LAI
var mapContainer = document.getElementById('map-selection');
if (mapContainer) {
    var defaultLat = 10.762622;
    var defaultLng = 106.660172;
    
    var map = L.map('map-selection').setView([defaultLat, defaultLng], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    var marker = L.marker([defaultLat, defaultLng]).addTo(map);

    map.on('click', function(e) {
        var lat = e.latlng.lat;
        var lng = e.latlng.lng;

        marker.setLatLng([lat, lng]);

        if(document.getElementById("ViDoNonAccount")) document.getElementById("ViDoNonAccount").value = lat;
        if(document.getElementById("KinhDoNonAccount")) document.getElementById("KinhDoNonAccount").value = lng;

        fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}&accept-language=vi`)
        .then(response => response.json())
        .then(data => {
            if (data && data.display_name) {
                let fullAddress = data.display_name;

                // XỬ LÝ CHÍ MẠNG: Loại bỏ dãy 5 chữ số (Mã bưu chính) và các dấu phẩy thừa kèm theo
                // Regex này tìm cụm 5 chữ số liên tiếp đứng tách biệt (\b\d{5}\b)
                fullAddress = fullAddress.replace(/,?\s*\b\d{5}\b/g, '');
                
                // Làm sạch lại các khoảng trắng hoặc dấu phẩy bị lặp lại nếu có sau khi xóa số
                fullAddress = fullAddress.replace(/,\s*,/g, ',').trim();

                var addressField = document.getElementById("AddressNonAccount");
                if (addressField) {
                    addressField.value = fullAddress;
                }
            }
        })
        .catch(err => console.error("Lỗi bản đồ:", err));
    });

    setTimeout(function () {
        map.invalidateSize(true);
    }, 400);
}

$('#BtnApplyVoucher').click(function () {
    var code = $('#VoucherCodeInput').val().trim();

    if (code === "") {
        $('#VoucherMessage').removeClass('d-none text-success').addClass('text-danger').html('Vui lòng nhập mã giảm giá!');
        return;
    }

    var danhSachItem = [];
    $('.cart-item-row').each(function() {
        var id = parseInt($(this).data('nongsan-id')) || 0;
        var gia = parseFloat($(this).data('don-gia')) || 0;
        var soluong = parseInt($(this).data('so-luong')) || 0;

        if (id > 0) {
            danhSachItem.push({
                NongSanId: id,
                SoLuongDat: soluong,
                DonGiaThoiDiem: gia
            });
        }
    });

    if (danhSachItem.length === 0) {
        alert("Không tìm thấy thông tin sản phẩm trong giỏ hàng để áp mã!");
        return;
    }

    $.ajax({
        // TRUYỀN CODE QUA URL ĐỂ CONTROLLER ĐÓN [FromQuery]
        url: '/Admin/KhuyenMai/CheckVoucher?code=' + encodeURIComponent(code),
        type: 'POST', // Ép buộc POST
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(danhSachItem), // Đóng gói mảng JSON
        success: function (res) {
            var $msg = $('#VoucherMessage');
            $msg.removeClass('d-none text-danger text-success');

            if (res.success) {
                $msg.addClass('text-success').html('<i class="bi bi-check-circle-fill"></i> ' + res.message);
                $('#HiddenKhuyenMaiId').val(res.khuyenMaiId);
                
                // Cập nhật số tiền giảm lên UI
                if ($('#DiscountDisplay').length) {
                    $('#DiscountDisplay').text('-' + res.soTienGiam.toLocaleString('vi-VN') + ' đ');
                }
                
                // Tính toán tổng số tiền cuối cùng
                var txtTotal = $('.checkout-summary-total').text() || '0'; 
                var temporaryTotal = parseFloat(txtTotal.replace(/[^0-9]/g, '')) || 0;
                var finalAmount = temporaryTotal - res.soTienGiam;
                
                if ($('#FinalAmountDisplay').length) {
                    $('#FinalAmountDisplay').text(finalAmount.toLocaleString('vi-VN') + ' đ');
                }
            } else {
                $msg.addClass('text-danger').html('<i class="bi bi-exclamation-triangle-fill"></i> ' + res.message);
                $('#HiddenKhuyenMaiId').val('');
                if ($('#DiscountDisplay').length) $('#DiscountDisplay').text('0 đ');
            }
        },
        error: function (xhr) {
            console.error(xhr.responseText);
            alert('Lỗi kết nối kiểm tra mã voucher! Hãy ấn F12 xem tab Console.');
        }
    });
});

$(document).on('click', '#BtnQuickApply', function() {
    var code = $(this).data('code');
    $('#VoucherCodeInput').val(code); // Tự điền mã vào ô text
    $('#BtnApplyVoucher').click();    // Tự kích hoạt nút kiểm tra tính tiền AJAX
});

