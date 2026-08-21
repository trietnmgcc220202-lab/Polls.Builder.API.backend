using RealtimeService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Tắt EventLog để tránh lỗi crash khi dừng service
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Cấu hình CORS tương thích hoàn toàn với SignalR và Gateway (Port 5005)
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

app.MapControllers();
app.MapHub<PollHub>("/hubs/polls");

// Render cấp port qua biến môi trường PORT — không hardcode 5003 nữa.
// Khi chạy local (không có biến PORT) sẽ tự fallback về 5003 như cũ.
var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Run($"http://0.0.0.0:{port}");