// [BACKEND] File: AccountService / Program.cs
using AccountService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Lấy Secret Key chuẩn (Đồng bộ tuyệt đối 100% với PollService)
var jwtSecretKey = builder.Configuration["Jwt:Key"] 
    ?? "MotDoanMaBaoMatRatDaiVaKhoDoanChoPollBuilder123!@#";

// 1. Database PostgreSQL (Neon)
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            
            // Tắt ValidateIssuer & Audience để tránh lệch cấu hình giữa các Service
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// 4. FIX LỖI: Kiểm tra và ép buộc tạo bảng "Users" nếu chưa có trong DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    var dbCreator = db.Database.GetService<IRelationalDatabaseCreator>();
    
    if (dbCreator != null && !dbCreator.HasTables())
    {
        dbCreator.CreateTables();
    }
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// 5. Health check
app.MapGet("/", () => Results.Ok("AccountService is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

// 6. Bind PORT của Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
