// Program.cs
using System;
using GaiaPrintAPI.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración de servicios ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Gaia Print API", Version = "v1" });
});

// CORS más seguro para producción (ajusta dominios)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",    // Angular dev
                "http://localhost:8080",    // Angular prod local (si aplica)
                "https://tienda.com"        // Tu dominio en producción (ajusta)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    // EventLog solo en Windows/server con permisos
    try
    {
        logging.AddEventLog(settings =>
        {
            settings.SourceName = "GaiaPrintAPI";
        });
    }
    catch
    {
        // Ignorar si no está disponible
    }
});

var app = builder.Build();

// --- Pipeline middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gaia Print API v1");
        c.RoutePrefix = "api-docs";
    });
}

// CORS y Authorization
app.UseCors("AllowAngularApp");
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Health endpoints (útil para Uptime checks)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/healthz", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

// --- Forzar escucha en el puerto que la plataforma asigne ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
// Asegurarnos de escuchar 0.0.0.0 para que esté accesible desde fuera del contenedor
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

// Iniciar la app
app.Run();
