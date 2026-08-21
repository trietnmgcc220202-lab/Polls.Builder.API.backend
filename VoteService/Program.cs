using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Neon / PostgreSQL (dùng chung database với PollService)
var conn = builder.Configuration.GetConnectionString("DefaultConnection"); // ← đã sửa
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddScoped<IVoteService, VoteService.Services.VoteService>();

builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Không cần EnsureCreated nữa vì PollService đã tạo bảng rồi
// using (var scope = app.Services.CreateScope()) { ... }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.MapControllers();

// Render cấp port qua biến môi trường PORT — đặt rõ ràng thay vì để mặc định.
// Khi chạy local (không có biến PORT) sẽ tự fallback về 5002.
var port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
app.Run($"http://0.0.0.0:{port}");