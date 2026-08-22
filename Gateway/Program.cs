using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Tắt EventLog của Windows để tránh lỗi crash khi dừng ứng dụng
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Load cấu hình ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 3. Cấu hình CORS linh hoạt cho cả Web và SignalR
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

// 4. Lấy PORT từ biến môi trường của Render (mặc định 8080 nếu chạy local)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// 5. Đặt UseCors TRƯỚC UseWebSockets và UseOcelot
app.UseCors("AllowFrontend");
app.UseWebSockets();

await app.UseOcelot();

app.Run();
