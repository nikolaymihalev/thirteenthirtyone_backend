using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using ThirteenThirtyOne.Application.DevelopmentGameplay;
using ThirteenThirtyOne.GameBackend.DevelopmentGameplay;
using ThirteenThirtyOne.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();
    builder.Services.AddScoped<IDevelopmentGameplayService, DevelopmentGameplayService>();
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
    // Swashbuckle's schema resolver reads MVC serializer options even for Minimal APIs.
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "13/31 Development Gameplay Harness",
        Version = "v1",
        Description = "Development-only, in-memory deterministic gameplay testing. All games disappear on restart.",
    }));
}

var app = builder.Build();

app.MapHealthChecks("/health/live");

if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentGames();
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Development / Gameplay"));
}

app.Run();

// Exposes the entry point to the in-process host integration tests.
public partial class Program;
