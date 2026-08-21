using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Tắt EventLog của Windows để tránh lỗi crash khi dừng ứng dụng
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Load cấu hình ocelot.json (giữ nguyên, JSON hợp lệ, giá trị mặc định là localhost)
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 3. Nạp biến môi trường SAU cùng để nó có quyền ưu tiên cao nhất,
//    tự động override Host/Port/Scheme khi deploy lên Render
//    (ví dụ set biến "Routes__1__DownstreamHostAndPorts__0__Host" trên Render).
//    Khi chạy local không set biến này, sẽ tự dùng giá trị localhost có sẵn trong ocelot.json.
builder.Configuration.AddEnvironmentVariables();

// 4. Cấu hình CORS linh hoạt cho cả Web và SignalR
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

await app.UseOcelot();

// 6. Render cấp port qua biến môi trường PORT — không được hardcode 5005.
//    Khi chạy local (không có biến PORT) sẽ tự fallback về 5005 như cũ.
var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Run($"http://0.0.0.0:{port}");