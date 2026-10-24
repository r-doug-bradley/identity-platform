using System.Globalization;
using Duende.IdentityModel;
using Duende.IdentityServer;
using IdentityServerAspNetIdentity.Data;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;

namespace IdentityServerAspNetIdentity;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Write most logs to the console but diagnostic data to a file.
        // See https://docs.duendesoftware.com/identityserver/diagnostics/data
        builder.Host.UseSerilog(
            (ctx, lc) =>
            {
                lc.WriteTo.Logger(consoleLogger =>
                {
                    consoleLogger.WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                        formatProvider: CultureInfo.InvariantCulture
                    );
                    if (builder.Environment.IsDevelopment())
                    {
                        consoleLogger.Filter.ByExcluding(
                            Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary")
                        );
                    }
                });
                if (builder.Environment.IsDevelopment())
                {
                    lc.WriteTo.Logger(fileLogger =>
                        {
                            fileLogger
                                .WriteTo.File(
                                    "./diagnostics/diagnostic.log",
                                    rollingInterval: RollingInterval.Day,
                                    fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                                    rollOnFileSizeLimit: true,
                                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                                    formatProvider: CultureInfo.InvariantCulture
                                )
                                .Filter.ByIncludingOnly(
                                    Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary")
                                );
                        })
                        .Enrich.FromLogContext()
                        .ReadFrom.Configuration(ctx.Configuration);
                }
            }
        );
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        builder.Services.AddTransient<IEmailSender, DevEmailSender>();

        var identityServerConnectionString = builder.Configuration.GetConnectionString(
            "DefaultConnection"
        );
        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(identityServerConnectionString)
        );

        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ASP.NET Identity's UserClaimsPrincipalFactory defaults to ClaimTypes.* (long URLs).
        // Duende's IdentityResource definitions use JwtClaimTypes.* (short names like "role"),
        // so without this mapping the role claim never matches the requested "roles" resource
        // and never reaches the token, even though AspNetUserRoles is populated.
        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
            options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
            options.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
            options.ClaimsIdentity.EmailClaimType = JwtClaimTypes.Email;
        });

        var idsBuilder = builder.Services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;

            // Use a large chunk size for diagnostic data in development where it will be redirected to a local file.
            if (builder.Environment.IsDevelopment())
            {
                options.Diagnostics.ChunkSize = 1024 * 1024 * 10; // 10 MB
            }
        });

        // Dev: persist tempkey.jwk to disk so local restarts keep the same signing key.
        // Non-dev: rely on Duende's automatic key management, which stores rotating keys in
        // idsrv.SigningKeys -- tokens survive App Service restarts and scale-outs without needing
        // a static cert. Auto-key-management requires a Business/Enterprise Duende license in
        // production; the dev signing key avoids that license warning in local work.
        if (builder.Environment.IsDevelopment())
        {
            idsBuilder.AddDeveloperSigningCredential();
        }

        idsBuilder
            .AddConfigurationStore(options =>
            {
                options.DefaultSchema = "idsrv";
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(
                        identityServerConnectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly)
                    );
            })
            .AddOperationalStore(options =>
            {
                options.DefaultSchema = "idsrv";
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(
                        identityServerConnectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly)
                    );
                options.EnableTokenCleanup = true;
            })
            .AddServerSideSessions()
            .AddAspNetIdentity<ApplicationUser>()
            .AddLicenseSummary();

        builder
            .Services.AddAuthentication()
            .AddOpenIdConnect(
                "oidc",
                "Sign-in with demo.duendesoftware.com",
                options =>
                {
                    options.SignInScheme =
                        IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.SaveTokens = true;

                    options.Authority = "https://demo.duendesoftware.com";
                    options.ClientId = "interactive.confidential";
                    options.ClientSecret = "secret";
                    options.ResponseType = "code";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role",
                    };
                }
            );

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}
