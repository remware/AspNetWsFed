using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Seller.Identity.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            return Ok("Welcome Home");
        }

        [Route("login")]
        public IActionResult SignIn()
        {
            if (User == null || !User.Identities.Any(identity => identity.IsAuthenticated))
            {
                var redirectUrl = Url.Action(nameof(SignIn));
                return Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUrl },
                    // OpenIdConnectDefaults.AuthenticationScheme
                    WsFederationDefaults.AuthenticationScheme);
            }           


            return RedirectToAction(nameof(AppNext));
        }

        [Authorize]
        [Route("app")]
        public IActionResult AppNext()
        {
            // check are we auth
            if (!User.Identity.IsAuthenticated && !User.Identity.AuthenticationType.Equals("AuthenticationTypes.Federation"))
            {
                return Forbid();
            }
    

            try
            {            
                if (!User.Identities.Any(identity => identity.HasClaim("http://schemas.microsoft.com/identity/claims/identityprovider", "live.com") || identity.HasClaim("rem", "true")))
                {
                    Response.WriteAsync($"Not authorized without claims for user {User.Identity.Name}");
                    return Forbid();
                }

                var currentClaimsIdentity = User.Identities.FirstOrDefault(identity =>
                    identity.HasClaim("http://schemas.microsoft.com/identity/claims/identityprovider", "live.com"));
                var claimRem = new Claim(ClaimTypes.Name, "rem.org");
                currentClaimsIdentity?.AddClaim(claimRem);
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(currentClaimsIdentity?.Claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role));

                SignIn(claimsPrincipal, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimLive = currentClaimsIdentity?.Claims.FirstOrDefault(id => id.Type.Equals("http://schemas.microsoft.com/identity/claims/displayname"));
                return Ok($"Welcome to Portal {claimLive?.Value}");
            }
            catch
            {
                return Unauthorized();
            }

           
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.WriteAsync($"Access Denied for user {User.Identity.Name}");
            return Unauthorized();
        }




    }
}