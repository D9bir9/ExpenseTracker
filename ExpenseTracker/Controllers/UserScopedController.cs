using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    public abstract class UserScopedController : Controller
    {
        protected string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user identifier is missing.");
    }
}
