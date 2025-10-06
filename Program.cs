
using GaiaPrintAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Configuración
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Gaia Print API", Version = "v1" });
});

// CORS más seguro para producción
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",    // Angular dev
                "http://localhost:8080",    // Angular prod
                "https://dominio.com"     // Tu dominio en producción
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventLog(settings =>
    {
        settings.SourceName = "GaiaPrintAPI";
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gaia Print API v1");
        c.RoutePrefix = "api-docs";
    });
}

app.UseCors("AllowAngularApp");
app.UseAuthorization();
app.MapControllers();

// Endpoint de health check
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();