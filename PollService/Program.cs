using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PollService.Data;
using PollService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddScoped<IPollService, PollService.Services.PollService>();

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

// JWT Authentication - Đã tối ưu nhận Token từ AccountService
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "MySuperSecretKeyForPollBuilderApp2026ExactKeyMustBe32CharsLong!")),
            
            // Tắt kiểm tra Issuer và Audience để tránh lỗi lệch tên miền giữa Render và Local/Gateway
            ValidateIssuer = false,
            ValidateAudience = false,
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapGet("/", () => Results.Ok("PollService is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
app.Run($"http://0.0.0.0:{port}");
