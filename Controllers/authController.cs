using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TiendaApi.Data;
using TiendaApi.Dtos;
using TiendaApi.Models;
using TiendaApi.Services;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TiendaDbContext _context;
    private readonly IConfiguration _config;
    private readonly EmailService _emailService;

    public AuthController(TiendaDbContext context, IConfiguration config, EmailService emailService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
    }

   [HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    try
    {
        // 1. Validar si ya existe
        if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == request.NombreUsuario || u.Correo == request.Correo))
            return BadRequest("El nombre de usuario o el correo ya están en uso.");

        bool esProfesional = request.TipoUsuario.ToLower() == "profesional";

        // 2. Crear el objeto usuario
       var usuario = new Usuario
    {
        NombreUsuario = request.NombreUsuario,
        Correo = request.Correo,
        ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena),
        Rol = request.Rol.ToLower(),
        TipoUsuario = request.TipoUsuario.ToLower(),
        Activo = request.TipoUsuario.ToLower() != "profesional", // False si es pro

        // 🔥 ESTO ES LO QUE TE FALTA ASIGNAR:
        NombreEmpresa = request.NombreEmpresa,
        Cif = request.Cif,
        Telefono = request.Telefono
    };

    _context.Usuarios.Add(usuario);
    await _context.SaveChangesAsync();
        // 3. Lógica de Emails si es Profesional
        if (esProfesional)
        {
            await EnviarCorreosRegistroProfesional(request);
            return Ok(new { 
                message = "Solicitud profesional recibida. Revisaremos tus datos (CIF/Empresa) y te avisaremos por correo cuando tu cuenta esté activa." 
            });
        }

        return Ok(new { message = "Usuario registrado correctamente." });
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Error interno al procesar el registro.");
    }
}
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);

    if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.ContrasenaHash))
        return Unauthorized("Credenciales incorrectas.");

    // 🔥 CORRECCIÓN: Solo bloqueamos si es PROFESIONAL y no está activo.
    // Los 'cliente' y 'admin' entrarán siempre (ya que su TipoUsuario no es "profesional")
    if (usuario.TipoUsuario.ToLower() == "profesional" && !usuario.Activo)
    {
        return BadRequest("Tu cuenta profesional aún está pendiente de validación por parte del administrador.");
    }

    var token = CrearToken(usuario);

    return Ok(new
    {
        token,
        rol = usuario.Rol,
        username = usuario.NombreUsuario,
        id = usuario.Id,
        tipoUsuario = usuario.TipoUsuario
    });
}

    private async Task EnviarCorreosRegistroProfesional(RegisterRequest datos)
    {
        // --- EMAIL PARA EL ADMIN (TÚ) ---
        string htmlAdmin = $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #e2e8f0; border-radius: 10px;'>
                <h2 style='color: #10b981;'>⚠️ Nueva Solicitud Profesional</h2>
                <p>Un nuevo usuario ha solicitado acceso profesional y necesita validación manual:</p>
                <hr>
                <p><b>Empresa:</b> {datos.NombreEmpresa}</p>
                <p><b>CIF/NIF:</b> {datos.Cif}</p>
                <p><b>Teléfono:</b> {datos.Telefono}</p>
                <p><b>Usuario:</b> {datos.NombreUsuario}</p>
                <p><b>Email:</b> {datos.Correo}</p>
                <hr>
                <p>Revisa los datos en el registro mercantil y activa al usuario desde el panel de control.</p>
            </div>";

        _emailService.Send("victorcoco2005@gmail.com", "🚀 Nueva solicitud de cuenta PRO - " + datos.NombreEmpresa, htmlAdmin);

        // --- EMAIL PARA EL CLIENTE ---
        string htmlCliente = $@"
            <div style='background-color: #f8fafc; padding: 40px; font-family: sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 20px; padding: 30px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                    <h1 style='color: #10b981;'>¡Hola {datos.NombreUsuario}!</h1>
                    <p style='color: #475569; font-size: 16px;'>Hemos recibido tu solicitud de cuenta <b>Profesional</b>.</p>
                    <p style='color: #475569;'>Nuestro equipo está verificando los datos de <b>{datos.NombreEmpresa}</b>. Este proceso suele tardar menos de 24h.</p>
                    <div style='background: #f1f5f9; padding: 15px; border-radius: 10px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 14px;'><b>Estado:</b> ⏳ Pendiente de validación de CIF</p>
                    </div>
                    <p style='font-size: 12px; color: #94a3b8;'>Te enviaremos otro correo en cuanto tu acceso sea aprobado.</p>
                </div>
            </div>";

        _emailService.Send(datos.Correo, "🎯 Recibida tu solicitud profesional - TuTienda", htmlCliente);
    }

    [Authorize(Roles = "admin")]
[HttpPatch("activar-profesional/{id}")]
public async Task<IActionResult> ActivarProfesional(int id)
{
    var usuario = await _context.Usuarios.FindAsync(id);
    if (usuario == null) return NotFound();

    usuario.Activo = true;
    await _context.SaveChangesAsync();

    // Opcional: Enviar email al usuario avisando que ya puede entrar
    _emailService.Send(usuario.Correo, "🚀 ¡Cuenta Activada!", 
        $"<h1>Hola {usuario.NombreUsuario}</h1><p>Tu cuenta profesional ha sido validada. Ya puedes iniciar sesión.</p>");

    return Ok(new { mensaje = "Usuario activado correctamente" });
}

[Authorize(Roles = "admin")]
[HttpGet("pendientes")]
public async Task<IActionResult> ObtenerPendientes()
{
    var pendientes = await _context.Usuarios
        .Where(u => u.TipoUsuario == "profesional" && !u.Activo)
        .Select(u => new { 
            id = u.Id, 
            nombreUsuario = u.NombreUsuario, 
            correo = u.Correo, 
            // 🔥 Fuerza la inclusión aquí:
            nombreEmpresa = u.NombreEmpresa, 
            cif = u.Cif, 
            telefono = u.Telefono 
        })
        .ToListAsync();

    return Ok(pendientes);
}
    private string CrearToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Role, usuario.Rol.ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Endpoints adicionales de perfil (manteniendo tu lógica)
    [Authorize]
    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var usuario = await _context.Usuarios
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.NombreUsuario, u.Correo, u.Rol, u.TipoUsuario })
            .FirstOrDefaultAsync();

        if (usuario == null) return NotFound();
        return Ok(usuario);
    }
}