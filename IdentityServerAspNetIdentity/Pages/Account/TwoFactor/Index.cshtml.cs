using System.Text;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.TwoFactor;

[SecurityHeaders]
[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public bool Found { get; private set; }
    public string? UserName { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public string FormattedKey { get; private set; } = string.Empty;
    public string AuthenticatorUri { get; private set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public sealed class InputModel
    {
        public string Code { get; set; } = string.Empty;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostResetKeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Found = false;
            return Page();
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Authenticator key reset.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Found = false;
            return Page();
        }

        var code = (Input.Code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code
        );

        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid code.");
            return await LoadAsync();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["RecoveryCodes"] = string.Join(" ", recoveryCodes ?? Array.Empty<string>());

        await _signInManager.RefreshSignInAsync(user);

        return RedirectToPage("./RecoveryCodes");
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Found = false;
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Two-factor authentication disabled.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Found = false;
            return Page();
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["RecoveryCodes"] = string.Join(" ", recoveryCodes ?? Array.Empty<string>());
        StatusMessage = "New recovery codes generated.";
        return RedirectToPage("./RecoveryCodes");
    }

    private async Task<IActionResult> LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Found = false;
            return Page();
        }

        Found = true;
        UserName = user.UserName;
        IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        FormattedKey = FormatKey(key ?? string.Empty);

        var email = await _userManager.GetEmailAsync(user);
        var issuer = "Keystone";
        AuthenticatorUri = GenerateQrCodeUri(issuer, email ?? user.UserName ?? user.Id, key ?? string.Empty);

        return Page();
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        for (var i = 0; i + 4 < unformattedKey.Length; i += 4)
        {
            result.Append(unformattedKey.AsSpan(i, 4)).Append(' ');
        }

        if (unformattedKey.Length % 4 != 0)
        {
            result.Append(unformattedKey.AsSpan(unformattedKey.Length - unformattedKey.Length % 4));
        }

        return result.ToString().ToLowerInvariant();
    }

    private static string GenerateQrCodeUri(string issuer, string accountName, string unformattedKey)
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            Uri.EscapeDataString(issuer),
            Uri.EscapeDataString(accountName),
            Uri.EscapeDataString(unformattedKey)
        );
    }
}
