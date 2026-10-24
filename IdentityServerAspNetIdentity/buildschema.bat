rmdir /S /Q "Data/Migrations"

dotnet ef migrations add Users -c ApplicationDbContext -o Data/Migrations

dotnet ef migrations add IdentityServerConfiguration -c ConfigurationDbContext -o Data/Migrations/IdentityServer/Configuration

dotnet ef migrations add IdentityServerPersistedGrants -c PersistedGrantDbContext -o Data/Migrations/IdentityServer/PersistedGrants
