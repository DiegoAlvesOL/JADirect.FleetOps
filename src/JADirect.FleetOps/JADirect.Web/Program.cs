using System.Threading.RateLimiting;
using JADirect.Application.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using JADirect.Data.Repositories;
using JADirect.Web.Middleware;
using Microsoft.AspNetCore.RateLimiting;

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

// ADICIONADO, Rate Limiting para proteger o endpoint de Login contra
//ataques de força bruta.
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("login-policy", fixedWindowOptions =>
    {
        fixedWindowOptions.PermitLimit = 10;
        fixedWindowOptions.Window = TimeSpan.FromMinutes(1);
        fixedWindowOptions.QueueLimit = 0;
        fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    
    //Resposta padrão quando o limite é atingido.
    // HTTP 429 Too Many Requests é o código semântico correto para rate limiting.
    // Redireciona para o Login em vez de retornar JSON, compatível com a View.
    rateLimiterOptions.OnRejected = async (context, CancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Redirect("/Account/Login?blocked=true");
        await Task.CompletedTask;
    };
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
builder.Services.AddScoped<DailyLogService>();


// Serviços (Camada de Aplicação)
builder.Services.AddScoped<FleetService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WalkaroundService>();
builder.Services.AddScoped<ChecklistItemRepository>();
builder.Services.AddScoped<BlockingRuleRepository>();



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
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();