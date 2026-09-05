var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");

app.Run();

// Exposes the entry point to the in-process host integration tests.
public partial class Program;
