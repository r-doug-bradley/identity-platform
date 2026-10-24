using System.ComponentModel.DataAnnotations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.ForgotPassword;

[SecurityHeaders]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public IndexModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string? UserName { get; set; }

        public string? ReturnUrl { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet(string? returnUrl)
    {
        Input = new InputModel { ReturnUrl = returnUrl };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByNameAsync(Input.UserName!);

        if (user == null)
        {
            return RedirectToPage("./Sent", new { returnUrl = Input.ReturnUrl });
        }

        var email = await _userManager.GetEmailAsync(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToPage("./Sent", new { returnUrl = Input.ReturnUrl });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Page(
            "/Account/ResetPassword/Index",
            pageHandler: null,
            values: new { userId = user.Id, code = token, returnUrl = Input.ReturnUrl },
            protocol: Request.Scheme
        );

        await _emailSender.SendEmailAsync(email, "Reset your password", $"Reset your password by clicking <a href='{resetUrl}'>here</a>.");

        return RedirectToPage("./Sent", new { returnUrl = Input.ReturnUrl });
    }
}
