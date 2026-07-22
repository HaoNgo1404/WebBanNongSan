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

document.addEventListener("DOMContentLoaded", function () {
    const chkUsePoints = document.getElementById("chkUsePoints");
    const pointsInputArea = document.getElementById("pointsInputArea");
    const inputDiemDung = document.getElementById("inputDiemDung");
    const hiddenDiemDung = document.getElementById("hiddenDiemDung");
    const btnApplyPoints = document.getElementById("btnApplyPoints");
    const pointsError = document.getElementById("pointsError");

    // SỬA TẠI ĐÂY: Thêm selector ".checkout-summary-total" để khớp chính xác với class trong _CheckoutSummary.cshtml
    const priceElement = document.querySelector(".checkout-summary-total") 
                       || document.querySelector(".text-end.fw-bold.fs-5") 
                       || document.querySelector(".fs-5.fw-bold"); 

    let rawPriceText = priceElement ? priceElement.innerText : "0";
    let tongTienGoc = parseInt(rawPriceText.replace(/[^0-9]/g, '')) || 0;

    // (Giữ nguyên phần logic sự kiện bên dưới của bạn...)
    if (chkUsePoints) {
        chkUsePoints.addEventListener("change", function () {
            if (this.checked) {
                pointsInputArea.style.display = "block";
                
                let maxPointsCuaKhach = parseInt(document.getElementById("maxPoints").innerText) || 0;
                
                // Điểm đề xuất = Giá trị nhỏ nhất giữa (Điểm hiện có) và (Tổng tiền đơn hàng)
                let diemDungDeXuat = Math.min(maxPointsCuaKhach, tongTienGoc);
                
                inputDiemDung.value = diemDungDeXuat;
                inputDiemDung.setAttribute("max", diemDungDeXuat); // Khóa nút bấm tăng mũi tên vượt mức
                
                apDungDiem(diemDungDeXuat);
            } else {
                pointsInputArea.style.display = "none";
                inputDiemDung.value = 0;
                apDungDiem(0);
            }
        });

        btnApplyPoints.addEventListener("click", function () {
            let maxPointsCuaKhach = parseInt(document.getElementById("maxPoints").innerText) || 0;
            let nhapDiem = parseInt(inputDiemDung.value) || 0;

            if (nhapDiem < 0) {
                pointsError.innerText = "Số điểm không được là số âm!";
                return;
            }
            if (nhapDiem > maxPointsCuaKhach) {
                pointsError.innerText = `Bạn chỉ có tối đa ${maxPointsCuaKhach} điểm!`;
                return;
            }
            
            // RÀNG BUỘC PHÍA CLIENT: Điểm nhập thủ công không được vượt quá tiền đơn hàng
            if (nhapDiem > tongTienGoc) {
                pointsError.innerText = `Số điểm dùng không được vượt quá giá trị đơn hàng (${tongTienGoc.toLocaleString('vi-VN')} đ)!`;
                return;
            }

            pointsError.innerText = "";
            apDungDiem(nhapDiem);
        });
    }

    function apDungDiem(diem) {
        hiddenDiemDung.value = diem;
        let tienSauGiam = tongTienGoc - diem;

        if (priceElement) {
            priceElement.innerHTML = tienSauGiam.toLocaleString('vi-VN') + " đ";
        }
    }
});

