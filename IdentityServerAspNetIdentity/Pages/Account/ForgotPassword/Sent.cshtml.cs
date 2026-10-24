using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.ForgotPassword;

[SecurityHeaders]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class SentModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet() { }
}
