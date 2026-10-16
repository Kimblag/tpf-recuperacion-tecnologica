using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Email;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPersonalService, PersonalService>();
builder.Services.AddScoped<IDonacionService, DonacionService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Contraseña, mínimo 8 caracteres, mayúscula, minúscula y dígito (sin símbolo obligatorio)
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;

        // Un correo solo puede pertenecer a una cuenta
        options.User.RequireUniqueEmail = true;

        // Bloqueo temporal tras 5 intentos fallidos
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<IdentityErrorDescriberEs>();

// el enlace de recuperación de pass vence en 1 hora
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(1));

// las políticas se basan en los roles de Identity, que coinciden con RolPersonal
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SoloAdministrador", policy =>
        policy.RequireRole(nameof(RolPersonal.Administrador)))
    .AddPolicy("PersonalInterno", policy =>
        policy.RequireRole(
            nameof(RolPersonal.Tecnico),
            nameof(RolPersonal.Coordinador),
            nameof(RolPersonal.Administrador)));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";

    // agrego config para que en producción la cookie solo viaja por HTTPS y en desarrollo sí se permite HTTP
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // los roles son datos de referencia: se aseguran siempre
    await DbInitializer.SeedRolesAsync(scope.ServiceProvider);

    // el primer Administrador y la configuración institucional se crean solo en desarrollo
    if (app.Environment.IsDevelopment())
    {
        await DbInitializer.SeedAdminAsync(scope.ServiceProvider);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
