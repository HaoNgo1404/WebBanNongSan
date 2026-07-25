using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebWeb.Controllers
{
    public class BaseController : Controller
    {
        protected int GetCurrentUserId()
        {
            int? sessionKhachHangId = HttpContext.Session.GetInt32("KhachHangId");
            if (sessionKhachHangId.HasValue && sessionKhachHangId.Value > 0)
            {
                return sessionKhachHangId.Value;
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var customClaim = User.FindFirst("KhachHangId");
                if (customClaim != null && int.TryParse(customClaim.Value, out int customId))
                {
                    return customId;
                }

                var clientIdentity = User.Identities.FirstOrDefault(id => id.AuthenticationType != "AdminScheme");
                if (clientIdentity != null)
                {
                    var userIdClaim = clientIdentity.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                    {
                        return id;
                    }
                }

                var backupClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (backupClaim != null && int.TryParse(backupClaim.Value, out int backupId))
                {
                    return backupId;
                }
            }
            return 0;
        }
    }
}