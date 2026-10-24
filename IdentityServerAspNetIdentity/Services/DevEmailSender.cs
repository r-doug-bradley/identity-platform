using Microsoft.AspNetCore.Identity.UI.Services;
using Serilog;

namespace IdentityServerAspNetIdentity.Services;

public sealed class DevEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Log.Information("Email to {Email} Subject={Subject} Body={Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
