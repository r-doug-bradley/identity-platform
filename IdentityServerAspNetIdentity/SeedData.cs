using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityServerAspNetIdentity.Data;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IdentityServerAspNetIdentity;

public class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
    using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var persistedGrantDbContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
    persistedGrantDbContext.Database.Migrate();

    var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
    configurationDbContext.Database.Migrate();

    foreach (var client in Config.Clients)
    {
      EnsureClient(configurationDbContext, client);
    }

    foreach (var resource in Config.IdentityResources)
    {
      if (!configurationDbContext.IdentityResources.Any(r => r.Name == resource.Name))
      {
        configurationDbContext.IdentityResources.Add(resource.ToEntity());
      }
    }

    foreach (var scopeResource in Config.ApiScopes)
    {
      if (!configurationDbContext.ApiScopes.Any(s => s.Name == scopeResource.Name))
      {
        configurationDbContext.ApiScopes.Add(scopeResource.ToEntity());
      }
    }

    foreach (var apiResource in Config.ApiResources)
    {
      if (!configurationDbContext.ApiResources.Any(r => r.Name == apiResource.Name))
      {
        configurationDbContext.ApiResources.Add(apiResource.ToEntity());
      }
    }

    configurationDbContext.SaveChanges();

    if (!app.Environment.IsDevelopment())
    {
      Log.Information("Skipping development user seed (alice/bob) in {Environment}", app.Environment.EnvironmentName);
      return;
    }

    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var alice = userMgr.FindByNameAsync("alice").Result;
    if (alice == null)
    {
      alice = new ApplicationUser
      {
        UserName = "alice",
        Email = "Alice.Smith@example.com",
        EmailConfirmed = true,
        PhoneNumber = "555-482-9910",
        PhoneNumberConfirmed = true,
      };

      var result = userMgr.CreateAsync(alice, "Pass123$").Result;
      if (!result.Succeeded)
      {
        throw new Exception(result.Errors.First().Description);
      }

      result = userMgr.AddClaimsAsync(alice, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Alice Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Alice"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.WebSite, "http://alice.example.com"),
                        }).Result;
      if (!result.Succeeded)
      {
        throw new Exception(result.Errors.First().Description);
      }
      Log.Debug("alice created");
    }
    else
    {
      Log.Debug("alice already exists");
    }

    var bob = userMgr.FindByNameAsync("bob").Result;
    if (bob == null)
    {
      bob = new ApplicationUser
      {
        UserName = "bob",
        Email = "BobSmith@example.com",
        EmailConfirmed = true
      };
      var result = userMgr.CreateAsync(bob, "Pass123$").Result;
      if (!result.Succeeded)
      {
        throw new Exception(result.Errors.First().Description);
      }

      result = userMgr.AddClaimsAsync(bob, new Claim[]
{
    new Claim(JwtClaimTypes.Name, "Bob Smith"),
    new Claim(JwtClaimTypes.GivenName, "Bob"),
    new Claim(JwtClaimTypes.FamilyName, "Smith"),
    new Claim(JwtClaimTypes.WebSite, "http://bob.example.com"),
    new Claim("location", "somewhere")
}).Result;

      if (!result.Succeeded)
      {
        throw new Exception(result.Errors.First().Description);
      }
      Log.Debug("bob created");
    }
    else
    {
      Log.Debug("bob already exists");
    }
  }

  // Idempotent client upsert. New clients are inserted via Duende's ToEntity() mapper.
  // Existing clients are reconciled additively across scopes, grant types, redirect URIs,
  // post-logout redirects, CORS origins, and secrets (matched by hashed value).
  // Edits to Config.cs that remove an item won't propagate — that's intentional, to avoid
  // accidentally yanking access from a live deployment via a seed run.
  private static void EnsureClient(ConfigurationDbContext db, Duende.IdentityServer.Models.Client model)
  {
    var existing = db.Clients
      .Include(c => c.AllowedScopes)
      .Include(c => c.AllowedGrantTypes)
      .Include(c => c.RedirectUris)
      .Include(c => c.PostLogoutRedirectUris)
      .Include(c => c.AllowedCorsOrigins)
      .Include(c => c.ClientSecrets)
      .SingleOrDefault(c => c.ClientId == model.ClientId);

    if (existing == null)
    {
      db.Clients.Add(model.ToEntity());
      return;
    }

    foreach (var s in model.AllowedScopes ?? Enumerable.Empty<string>())
      if (!existing.AllowedScopes.Any(x => x.Scope == s))
        existing.AllowedScopes.Add(new ClientScope { Scope = s });

    foreach (var g in model.AllowedGrantTypes ?? Enumerable.Empty<string>())
      if (!existing.AllowedGrantTypes.Any(x => x.GrantType == g))
        existing.AllowedGrantTypes.Add(new ClientGrantType { GrantType = g });

    foreach (var r in model.RedirectUris ?? Enumerable.Empty<string>())
      if (!existing.RedirectUris.Any(x => x.RedirectUri == r))
        existing.RedirectUris.Add(new ClientRedirectUri { RedirectUri = r });

    foreach (var r in model.PostLogoutRedirectUris ?? Enumerable.Empty<string>())
      if (!existing.PostLogoutRedirectUris.Any(x => x.PostLogoutRedirectUri == r))
        existing.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = r });

    foreach (var o in model.AllowedCorsOrigins ?? Enumerable.Empty<string>())
      if (!existing.AllowedCorsOrigins.Any(x => x.Origin == o))
        existing.AllowedCorsOrigins.Add(new ClientCorsOrigin { Origin = o });

    foreach (var secret in model.ClientSecrets ?? Enumerable.Empty<Duende.IdentityServer.Models.Secret>())
      if (!existing.ClientSecrets.Any(x => x.Value == secret.Value))
        existing.ClientSecrets.Add(new ClientSecret
        {
          Value = secret.Value,
          Type = secret.Type ?? "SharedSecret",
          Description = secret.Description,
          Expiration = secret.Expiration
        });
  }
}
