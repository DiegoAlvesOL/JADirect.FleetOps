using JADirect.Application.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using JADirect.Data.Repositories; // Namespace dos repositórios

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. CONFIGURAÇÃO DE SERVIÇOS (CONTAINER)
// ---------------------------------------------------------

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(8);
    });

// ---------------------------------------------------------
// 2. INJEÇÃO DE DEPENDÊNCIA
// ---------------------------------------------------------

// Infraestrutura
builder.Services.AddScoped<JADirect.Data.Infrastructure.DbConnectionFactory>(sp =>
    new JADirect.Data.Infrastructure.DbConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")
                                                         ?? throw new Exception("Connection String not found.")));

// Repositórios (Camada de Dados - SQL Puro)
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddScoped<InspectionRepository>();
builder.Services.AddScoped<DailyLogRepository>();


// Serviços (Camada de Aplicação)
builder.Services.AddScoped<FleetService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// ---------------------------------------------------------
// 3. MIDDLEWARES (PIPELINE DE EXECUÇÃO)
// ---------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();