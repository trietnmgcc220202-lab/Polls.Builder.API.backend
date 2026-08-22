using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOcelot(builder.Configuration);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseWebSockets();

app.MapGet("/", () => Results.Ok("Gateway is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await app.UseOcelot();
app.Run();