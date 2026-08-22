using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VoteService.Data;
using VoteService.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. PostgreSQL Connection
string? conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException("LỖI: Chưa khai báo 'DefaultConnection'!");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddScoped<IVoteService, VoteService.Services.VoteService>();

// 4. CORS Policy
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 5. JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// 6. Port linh hoạt từ Render
string port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

WebApplication app = builder.Build();

// 7. Bật Swagger trên cả Production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VoteService API v1");
});

// 8. Endpoint Health Check
app.MapGet("/", () => Results.Ok("VoteService API is running!"));

app.UseCors("AllowAll");

app.UseAuthentication(); // <-- Bắt buộc phải có để xài Token
app.UseAuthorization();

app.MapControllers();

app.Run();