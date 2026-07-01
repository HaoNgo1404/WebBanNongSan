using Microsoft.AspNetCore.Mvc;

namespace WebWeb.Controllers
{
    public class NotificationController : Controller
    {
        // Trang thông báo đặt hàng lẻ thành công
        public IActionResult OrderSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        // Trang thông báo lỗi (Nếu có)
        public IActionResult OrderFailed()
        {
            return View();
        }
    }
}