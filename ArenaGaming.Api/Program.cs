using System.Text.Json.Serialization;
using ArenaGaming.Api.Configuration;
using ArenaGaming.Api.Middleware;
using ArenaGaming.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Configure CORS for WebSocket connections
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add Pulsar and notification services
builder.Services.AddPulsarServices(builder.Configuration);

// Add health check services
builder.Services.AddSingleton<ArenaGaming.Api.Services.IHealthCheckResultsService, ArenaGaming.Api.Services.HealthCheckResultsService>();
builder.Services.AddHostedService<ArenaGaming.Api.Services.StartupHealthCheckService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Note: Database migrations are now handled automatically by StartupHealthCheckService
app.Run();