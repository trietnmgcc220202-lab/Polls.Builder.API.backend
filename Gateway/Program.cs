using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Tắt EventLog của Windows để tránh lỗi crash khi dừng ứng dụng
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Load cấu hình ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 3. Nạp biến môi trường SAU cùng để có quyền ưu tiên cao nhất
builder.Configuration.AddEnvironmentVariables();

// 4. Cấu hình CORS
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

var app = builder.Build();

// 5. Đặt UseCors TRƯỚC UseWebSockets và UseOcelot
app.UseCors("AllowFrontend");

app.UseWebSockets();

// === BỔ SUNG: Xử lý Health Check từ Render để không bị đẩy vào Ocelot gây log đỏ ===
app.MapGet("/", () => "Gateway is running!");
app.MapMethods("/", new[] { "HEAD" }, () => Results.Ok());
// =================================================================================

await app.UseOcelot();

// 6. Render cấp port qua biến môi trường PORT — không được hardcode 5005.
var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Run($"http://0.0.0.0:{port}");
