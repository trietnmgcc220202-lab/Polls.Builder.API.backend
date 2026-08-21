using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Tắt EventLog của Windows để tránh lỗi crash khi dừng ứng dụng
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Load cấu hình ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 3. Nạp biến môi trường SAU cùng
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

app.UseCors("AllowFrontend");
app.UseWebSockets();

// === CẮM MIDDLEWARE CHẶN HEALTH CHECK TRƯỚC OCELOT ===
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.StatusCode = 200;
        if (context.Request.Method == "GET")
        {
            await context.Response.WriteAsync("Gateway is running!");
        }
        return; // Kết thúc request tại đây, không cho lọt xuống Ocelot
    }
    await next();
});
// ===================================================

await app.UseOcelot();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Run($"http://0.0.0.0:{port}");
