using TiendaApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TiendaApi.Models;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Dtos;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TiendaDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(TiendaDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public IActionResult SoloAdmin()
    {
        return Ok("Este es un endpoint solo para admins.");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
         try
        {
        if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == request.NombreUsuario || u.Correo == request.Correo))
            return BadRequest("El usuario ya existe.");

        var usuario = new Usuario
        {
            NombreUsuario = request.NombreUsuario,
            Correo = request.Correo,
            ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena),
            Rol = request.Rol.ToLower()
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Usuario registrado correctamente." });
       
            // tu código
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }

       


    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.ContrasenaHash))
            return Unauthorized("Credenciales inválidas.");

        var token = CrearToken(usuario);

        return Ok(new
        {
            token,
            rol = usuario.Rol,
            username = usuario.NombreUsuario, // Agregar nombre de usuario al token
            id = usuario.Id
        });
    }
    
    [Authorize]
    [HttpPut("perfil")]
    public async Task<IActionResult> EditarPropioPerfil([FromBody] Usuario datos)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            usuario.NombreUsuario = datos.NombreUsuario;
            usuario.Correo = datos.Correo;

            await _context.SaveChangesAsync();
            return Ok("Perfil actualizado.");
        }

  [Authorize]
    [HttpPut("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordRequest2 request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.ContrasenaHash))
                return BadRequest("La contraseña actual es incorrecta.");
                 usuario.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
            await _context.SaveChangesAsync();
             return Ok("Contraseña actualizada correctamente.");
}

   [HttpPost("recuperar-password")]
public async Task<IActionResult> RecuperarPassword([FromBody] ChangePasswordRequest request)
{
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);
    if (usuario == null)
        return BadRequest("No existe ninguna cuenta con ese correo.");

    usuario.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
    await _context.SaveChangesAsync();

    return Ok("Contraseña actualizada correctamente.");
}

    [Authorize]
    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.NombreUsuario, u.Correo, u.Rol })
                .FirstOrDefaultAsync();

            if (usuario == null) return NotFound();

            return Ok(usuario);
        }

    private string CrearToken(Usuario usuario)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), // ID de usuario
        new Claim(ClaimTypes.Name, usuario.NombreUsuario),
        new Claim(ClaimTypes.Role, usuario.Rol.ToLower()) // rol en minúsculas para evitar problemas
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
}
