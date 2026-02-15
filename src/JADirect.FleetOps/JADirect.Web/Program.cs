using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JADirect.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);



// ---------------------------------------------------------
// 1. CONFIGURAÇÃO DE SERVIÇOS (CONTAINER)
// ---------------------------------------------------------

// Suporte para MVC (Controllers e Views)
builder.Services.AddControllersWithViews();

// Configuração de autenticação via Cookies
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

// Registra a Factory de Conexão com a String do appsettings.json
// Adicione as dependências (Injeção de Dependência)
builder.Services.AddScoped<JADirect.Data.Infrastructure.DbConnectionFactory>(sp =>
    new JADirect.Data.Infrastructure.DbConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")
                                                         ?? throw new Exception("Connection String not found.")));


// Registra os Repositórios (Camada de Dados)
builder.Services.AddScoped<JADirect.Data.Repositories.UserRepository>();
builder.Services.AddScoped<JADirect.Data.Repositories.VehicleRepository>();

// Registra os Serviços (Camada de Aplicação)
builder.Services.AddScoped<JADirect.Application.Services.AuthService>();



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
app.UseRouting();


//Essencial: Primeiro Authentication e depois Authorization
app.UseAuthentication();
app.UseAuthorization();

// Configuração de rota padrão.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();