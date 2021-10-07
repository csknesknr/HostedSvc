using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HostedSvc.Controllers
{
    public class AccountController : Controller
    {
        [NonAction]
        private ClaimsPrincipal UserPrincipal()
        {
            IEnumerable<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "UserNameIdentifier"),
                new Claim(ClaimTypes.Name, "UserName"),
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }

        public async Task<IActionResult> Login()
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipal());

            // Enable ajax to send authentication cookie in subsequent requests
            this.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            this.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");

            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Content("Logout complete!");
        }
    }
}
