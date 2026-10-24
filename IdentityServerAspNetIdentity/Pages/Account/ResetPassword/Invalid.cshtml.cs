using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.ResetPassword;

[SecurityHeaders]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class InvalidModel : PageModel
{
    public void OnGet() { }
}
