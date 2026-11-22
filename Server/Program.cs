using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Server.Data;
using Server.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("Missing JWT secret");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SmartBusServer",
        ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "SmartBusClient",
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var idClaim = context.Principal?.FindFirst("id")?.Value;
            var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!int.TryParse(idClaim, out var id)) { context.Fail("Invalid user"); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<SmartBusDbContext>();
            if (!await db.NguoiDungs.AnyAsync(x => x.id == id && x.trangThai && x.vaiTro == role)) context.Fail("Account disabled or role changed");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SmartBusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CanhBao] Khong the ket noi Redis: {ex.Message}");
    }
}

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped<INguoiDungService, NguoiDungService>();
builder.Services.AddHttpClient<CloudinaryMinhChung>(http =>
{
    http.Timeout = TimeSpan.FromSeconds(60);
    http.MaxResponseContentBufferSize = 5 * 1024 * 1024;
});
builder.Services.AddScoped<IXacThucService, XacThucService>();
builder.Services.AddScoped<IVanHanhService, VanHanhService>();
builder.Services.AddSingleton<IMatMaService, MatMaService>();
builder.Services.AddSingleton<SigningKeyService>();
builder.Services.AddSingleton<OfflineQrValidationService>();
builder.Services.AddScoped<OfflineSyncService>();
builder.Services.AddScoped<IVeService, VeService>();
builder.Services.AddScoped<ISoatVeService, SoatVeService>();
builder.Services.AddScoped<IBaoCaoService, BaoCaoService>();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SmartBusDbContext>();
        await DuLieuKhoiTao.KhoiTaoDuLieuMau(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CanhBao] Loi khoi tao du lieu ban dau: {ex.Message}");
    }
}

app.MapControllers();
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, thoiGian = DateTime.UtcNow }));

app.Run();
