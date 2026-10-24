using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.ResetPassword;

[SecurityHeaders]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class DoneModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet() { }
}
