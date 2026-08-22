using AccountService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Chuỗi Secret Key dùng chung đồng bộ
string jwtSecretKey = "MotDoanMaBaoMatRatDaiVaKhoDoanChoPollBuilder123!@#";

// 2. Database PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Cấu hình JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// 5. Tự động tạo Bảng trong Database nếu chưa tồn tại (Tránh lỗi 500 do thiếu Table)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var dbCreator = db.Database.GetService<IRelationalDatabaseCreator>();
        if (dbCreator != null && !dbCreator.HasTables())
        {
            dbCreator.CreateTables();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Lỗi khởi tạo DB: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// 6. Health check
app.MapGet("/", () => Results.Ok("AccountService is running"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
