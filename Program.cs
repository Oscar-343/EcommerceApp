using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgresql")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Login social: Google y GitHub.
// Los providers solo se registran si existen credenciales configuradas,
// de modo que la app sigue funcionando aunque falten.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

if (!string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGitHub(options =>
        {
            options.ClientId = githubClientId;
            options.ClientSecret = githubClientSecret;
            options.Scope.Add("user:email");
        });
}

builder.Services.AddControllersWithViews();

// Envío de correos (SMTP configurable; sin SMTP imprime en consola en desarrollo).
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

// Subida de imágenes a Supabase Storage (usado desde el panel de administración).
builder.Services.AddHttpClient<IImageStorageService, SupabaseImageStorageService>();

// Cálculo de los reportes del panel admin (ingresos, reservas, ventas, inventario).
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Render coloca la app detrás de un proxy inverso que no es loopback,
// así que hay que vaciar KnownNetworks/KnownProxies para que ASP.NET
// confíe en los headers X-Forwarded-* que manda ese proxy. Sin esto,
// UseHttpsRedirection y las URLs generadas (login social, "olvidé mi
// contraseña") no ven el esquema https real y salen con http.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// Crear roles por defecto solo si no existen (evita errores al reiniciar la app)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
            if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed de productos de ejemplo (solo si no hay productos)
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await ProductSeeder.SeedProductsAsync(context);
    await ServiceSeeder.SeedServicesAsync(context);

    // Cuenta de administración
    await AdminSeeder.SeedAdminAsync(userManager, roleManager, builder.Configuration);

    // Seed de pedidos y reservas de prueba, para probar los reportes
    await OrderReservationSeeder.SeedOrdersAndReservationsAsync(context, userManager);
}

app.Run();