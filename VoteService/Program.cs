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

// Cấu hình CORS cho phép Credentials (Cookie/Token)
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
app.Run($"http://0.0.0.0:{port}");
