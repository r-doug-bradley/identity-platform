using System.ComponentModel.DataAnnotations;
using System.Linq;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Login;

[SecurityHeaders]
[AllowAnonymous]
public class TwoFactor : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;

    public TwoFactor(
        IIdentityServerInteractionService interaction,
        IEventService events,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _interaction = interaction;
        _events = events;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required]
        public string ReturnUrl { get; set; } = "~/";

        public bool RememberLogin { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(string? returnUrl, bool rememberLogin)
    {
        Input = new InputModel
        {
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "~/" : returnUrl,
            RememberLogin = rememberLogin,
            RememberMachine = false,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            ErrorMessage = "Your two-factor session has expired. Please log in again.";
            return Page();
        }

        var code = (Input.Code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);

        Microsoft.AspNetCore.Identity.SignInResult result;
        if (code.Length >= 12 && code.All(char.IsLetterOrDigit))
        {
            result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);
        }
        else
        {
            result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, Input.RememberLogin, Input.RememberMachine);
        }

        if (result.Succeeded)
        {
            var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));
            Telemetry.Metrics.UserLogin(context?.Client.ClientId, IdentityServerConstants.LocalIdentityProvider);

            if (context != null && context.IsNativeClient())
            {
                return this.LoadingPage(Input.ReturnUrl);
            }

            return Redirect(Input.ReturnUrl);
        }

        const string error = "invalid 2fa code";
        var ctx = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, error, clientId: ctx?.Client.ClientId));
        Telemetry.Metrics.UserLoginFailure(ctx?.Client.ClientId, IdentityServerConstants.LocalIdentityProvider, error);

        ErrorMessage = "Invalid code.";
        return Page();
    }
}
