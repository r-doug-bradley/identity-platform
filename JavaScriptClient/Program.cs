using Duende.Bff;
using Duende.Bff.Yarp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services
.AddBff()
.ConfigureOpenIdConnect(options =>
{
  //options.Authority = "https://keystone.identity.local:5001";
  options.Authority = "https://localhost:5001";
  options.ClientId = "bff";
  options.ClientSecret = "secret";
  options.ResponseType = "code";
  
  options.Scope.Add("api1");
  options.Scope.Add("offline_access");
  options.SaveTokens = true;
  
  options.GetClaimsFromUserInfoEndpoint = true;
  options.MapInboundClaims = false;
  options.PushedAuthorizationBehavior =
    Microsoft.AspNetCore.Authentication.OpenIdConnect
    .PushedAuthorizationBehavior.Disable;
})
.ConfigureCookies(options => { })
.AddRemoteApis();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseBff();

app.UseAuthorization();
app.MapBffManagementEndpoints();

app.Run();