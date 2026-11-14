# Keystone

A working identity platform on .NET 10: Duende IdentityServer backed by ASP.NET Core Identity and
SQL Server, plus the three kinds of client you meet in practice. Everything is wired and runs
locally end to end; nothing is mocked.

It started from Duende's quickstart samples and keeps their MIT headers where their code remains.
The parts that go beyond the quickstarts are the ones you end up needing anyway: EF Core migrations
for all three stores, idempotent seeding, server-side sessions, claim-type alignment between
ASP.NET Identity and IdentityServer, a BFF for the browser client, and a deploy workflow that
refuses to ship a password.

## What is in here

| Project | Port | Role |
|---|---|---|
| `IdentityServerAspNetIdentity` | 5001 | The identity server. ASP.NET Core Identity users, Duende configuration and operational stores in SQL Server (schema `idsrv`), server-side sessions, an external OIDC login against Duende's demo server, Serilog. |
| `Api` | 6001 | A minimal API protected by JWT bearer tokens. One endpoint, `/identity`, that echoes the caller's claims and requires the `api1` scope. |
| `Client` | | A console app doing the client-credentials flow and calling the API. The smallest proof the server works. |
| `WebClient` | 5002 | A Razor Pages app signing in with authorization code + PKCE, requesting `api1`, `offline_access` and a custom `verification` scope. |
| `JavaScriptClient` | 5003 | A browser app behind a Duende BFF. Tokens never reach the browser; the BFF holds them and proxies API calls through YARP. |

The clients, scopes and resources are declared once in `Config.cs` and written to the database
by the seeder, so the running server reads them from SQL Server, not from code.

## Run it

Prerequisites: .NET 10 SDK, SQL Server (LocalDB is fine), trusted dev certificates
(`dotnet dev-certs https --trust`).

1. Put a connection string in `IdentityServerAspNetIdentity/appsettings.json` under
   `ConnectionStrings:DefaultConnection`, or in user secrets. The committed value is empty on
   purpose.
2. Seed. This applies all migrations, writes the clients and scopes, and in Development creates
   the two demo users (`alice` and `bob`, password `Pass123$`):

   ```
   dotnet run --project IdentityServerAspNetIdentity -- /seed
   ```

3. Start the server, then whichever clients you want, each in its own terminal:

   ```
   dotnet run --project IdentityServerAspNetIdentity
   dotnet run --project Api
   dotnet run --project WebClient
   dotnet run --project JavaScriptClient
   dotnet run --project Client
   ```

   The console client prints an access token and the claims the API returned. The web client
   opens at https://localhost:5002 and the BFF app at https://localhost:5003; sign in as `alice`.

## Things worth knowing

- **Claim types.** ASP.NET Identity issues long `ClaimTypes.*` URIs; IdentityServer's resources
  use short JWT names. `HostingExtensions.cs` maps `sub`, `name`, `role` and `email` so roles
  actually reach the token. Without that, `AspNetUserRoles` fills up and the token stays empty.
- **Signing keys.** Development uses a persisted developer key so restarts keep the same key.
  Outside Development the code relies on Duende's automatic key management, which stores rotating
  keys in `idsrv.SigningKeys` and survives restarts and scale-out without a certificate.
- **Secrets.** Every client secret in the repo is the literal word `secret`, as in the Duende
  samples. They are placeholders for local use. The `UserSecretsId` in the server project is a
  key for the local user-secrets store, not a credential.
- **Deploy.** `.github/workflows/deploy-identityserver.yml` publishes the server to an Azure App
  Service. It is manual-only, the app name is a placeholder, and it fails the build if a password
  appears in any `appsettings*.json`.
- **Licensing.** Duende IdentityServer is free for development, testing and small organisations
  under its Community Edition; production use beyond that needs a Duende license, and automatic
  key management is a Business-tier feature. The code in this repository is MIT.

## License

MIT. See `LICENSE`. Files derived from Duende's quickstarts carry their original MIT header.
