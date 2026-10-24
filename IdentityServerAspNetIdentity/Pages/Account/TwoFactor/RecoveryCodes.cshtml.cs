using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.TwoFactor;

[SecurityHeaders]
[Authorize]
public class RecoveryCodesModel : PageModel
{
    public List<string> Codes { get; private set; } = new();

    public void OnGet()
    {
        var raw = TempData["RecoveryCodes"] as string;
        Codes = string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
