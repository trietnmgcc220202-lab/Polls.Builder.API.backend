using RealtimeService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Tắt EventLog để tránh lỗi crash khi dừng service
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Cấu hình CORS tương thích hoàn toàn với SignalR và Gateway
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

var app = builder.Build();

app.UseCors("AllowFrontend");

// Health check endpoints (để Render health check pass)
app.MapGet("/", () => Results.Ok("RealtimeService is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();
app.MapHub<PollHub>("/hubs/polls");

// Render cấp port qua biến môi trường PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Run($"http://0.0.0.0:{port}");
