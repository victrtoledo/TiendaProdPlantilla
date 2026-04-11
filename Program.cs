using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TiendaApi.Data;
using TiendaApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 DB (en producción vendrá de Azure)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=tienda.db";

builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseSqlite(connectionString)
);

// 🔹 JWT (desde Azure App Settings)
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
    options.RequireHttpsMetadata = true; // 🔥 obligatorio en Azure
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

        // 🔥 ahora sí usamos issuer/audience
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ClockSkew = TimeSpan.Zero
    };
});

// 🔹 Controllers
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 Email
builder.Services.AddScoped<EmailService>();

// 🔹 CORS (más seguro que AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://plantillaecommerce-f5dhckf7acbkd0fe.spaincentral-01.azurewebsites.net"
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

// 🔥 Angular (SPA)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔥 CLAVE PARA ANGULAR (SPA routing)
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
    db.Database.EnsureCreated(); // 👈 IMPORTANTE
}

app.Run();