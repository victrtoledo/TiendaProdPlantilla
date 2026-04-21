using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TiendaApi.Data;
using TiendaApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=tienda.db";

builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseSqlite(connectionString)
);

// 🔹 JWT
var jwtSecretKey = builder.Configuration["Jwt:Key"]
                   ?? throw new Exception("JWT Key no configurada");

var keyBytes = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // ← false en dev, true en prod
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

// 🔹 Controllers
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Services
builder.Services.AddScoped<EmailService>();

// 🔹 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
       policy.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200", // ← añadir
            "https://x20k.com",               // Tu dominio oficial
            "https://www.x20k.com",           // Con www
            "https://x20k1.azurewebsites.net"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// 🔹 Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔥 SPA Angular
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔥 Angular routing fallback
app.MapFallbackToFile("index.html");

// 🔥 BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();

    // Crear carpeta /home/data si no existe (Azure)
    var connectionStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    if (connectionStr.Contains("/home/data"))
    {
        var dbDir = "/home/data";
        if (!Directory.Exists(dbDir))
            Directory.CreateDirectory(dbDir);
    }

    db.Database.Migrate();
}

app.Run();