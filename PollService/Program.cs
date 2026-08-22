// [BACKEND] File: PollService / Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using PollService.Data;
using PollService.Services;

// 1. Tắt tự động mapping claim để giữ nguyên "nameid", "email" từ AccountService
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context (PostgreSQL Neon)
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

// Dependency Injection
builder.Services.AddScoped<IPollService, PollService.Services.PollService>();

// Cấu hình CORS
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

// Lấy Secret Key chuẩn (Đồng bộ tuyệt đối với AccountService)
var jwtSecretKey = builder.Configuration["Jwt:Key"] 
    ?? "MotDoanMaBaoMatRatDaiVaKhoDoanChoPollBuilder123!@#";

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            
            // Tắt kiểm tra Issuer, Audience & Lifetime để loại trừ lỗi tên miền và lệch múi giờ trên Render
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false, 
            ClockSkew = TimeSpan.Zero
        };

        // BẮT BỆNH: In lý do chi tiết ra Render Logs nếu Token bị từ chối
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ [JWT ERROR]: Auth thất bại -> {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"⚠️ [JWT CHALLENGE]: Request bị từ chối (401) -> Error: {context.Error}, Description: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware Pipeline
app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoints
app.MapGet("/", () => Results.Ok("PollService is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

// Render Dynamic Port
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
app.Run($"http://0.0.0.0:{port}");
