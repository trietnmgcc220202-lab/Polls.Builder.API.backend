using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddScoped<IVoteService, VoteService.Services.VoteService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Bật Swagger trên cả Render để test trực tiếp
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowAll");

// Trang kiểm tra nhanh dịch vụ
app.MapGet("/", () => "VoteService is RUNNING!");

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
app.Run($"http://0.0.0.0:{port}");
