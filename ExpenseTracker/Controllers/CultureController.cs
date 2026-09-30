
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;


namespace ExpenseTracker.Controllers
{

    public class CultureController : Controller
    {
        [HttpPost]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            var allowedCultures = new[] { "en-US", "en-GB", "en-NG" };
            if (!allowedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            {
                culture = "en-US";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}