using System.ComponentModel.DataAnnotations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Account.ResetPassword;

[SecurityHeaders]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public sealed class InputModel
    {
        [Required]
        public string? UserId { get; set; }

        [Required]
        public string? Code { get; set; }

        public string? ReturnUrl { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(string? userId, string? code, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("./Invalid");
        }

        Input = new InputModel
        {
            UserId = userId,
            Code = code,
            ReturnUrl = returnUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.UserId!);
        if (user == null)
        {
            return RedirectToPage("./Done", new { returnUrl = Input.ReturnUrl });
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Code!, Input.Password!);
        if (result.Succeeded)
        {
            return RedirectToPage("./Done", new { returnUrl = Input.ReturnUrl });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
