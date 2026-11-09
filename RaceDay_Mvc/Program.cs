using System.Globalization;
using RaceDay.Mvc.Services;

// Decimal points in forms/JSON must not depend on the machine's locale (en-ZA uses a comma).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.Name = ".RaceDay.Mvc.Session";
});

// ApiBaseUrl comes from appsettings.json locally, or from the ApiBaseUrl environment variable in Docker.
var apiBase = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

builder.Services.AddHttpClient<ApiClient>(c =>
    {
        c.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    // UseCookies=false: each user's API cookie is forwarded by ApiClient, so users never share a cookie jar.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false,
        // Local dev only: accept the API's self-signed dev certificate.
        ServerCertificateCustomValidationCallback = builder.Environment.IsDevelopment()
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : null
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Switched off inside Docker (plain HTTP) via the UseHttpsRedirection=false environment variable.
if (app.Configuration.GetValue("UseHttpsRedirection", true))
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
