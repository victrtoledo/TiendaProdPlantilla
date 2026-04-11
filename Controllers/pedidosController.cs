using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TiendaApi.Data;
using TiendaApi.Models;
using TiendaApi.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin, cliente")]
public class PedidosController : ControllerBase
{
    private readonly TiendaDbContext _context;
    private readonly EmailService _emailService;

    public PedidosController(TiendaDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // GET: api/pedidos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userRole == "admin")
        {
            var pedidos = await _context.Pedidos
        .Include(p => p.Detalles)
            .ThenInclude(d => d.Producto)
        .Include(p => p.Detalles)
            .ThenInclude(d => d.Variante) // ← añadir
        .Include(p => p.Usuario)
            .ThenInclude(u => u.DetalleUsuario)
        .ToListAsync();

            return Ok(pedidos);
        }

        var pedidosUsuario = await _context.Pedidos
        .Where(p => p.UsuarioId == userId)
        .Include(p => p.Detalles)
            .ThenInclude(d => d.Producto)
        .Include(p => p.Detalles)
            .ThenInclude(d => d.Variante) // ← añadir
        .ToListAsync();

        return Ok(pedidosUsuario);
    }

    // GET: api/pedidos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Pedido>> GetPedido(int id)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        var pedido = await _context.Pedidos
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound();

        if (userRole != "admin" && pedido.UsuarioId != userId)
            return Forbid();

        return Ok(pedido);
    }

    // POST: api/pedidos
    [HttpPost]
    public async Task<ActionResult<Pedido>> CrearPedido(Pedido pedido)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);
        pedido.UsuarioId = userId;

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido);
    }

    // POST: checkout (crear pedido desde carrito)
    [HttpPost("exito")]
    public async Task<IActionResult> Checkout()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        var carritoItems = await _context.CarritoItems
            .Where(c => c.UsuarioId == userId)
            .Include(c => c.Producto)
            .ToListAsync();

        if (!carritoItems.Any())
            return BadRequest("Carrito vacío");

        var pedido = new Pedido
        {
            UsuarioId = userId,
            Fecha = DateTime.UtcNow,
            Total = carritoItems.Sum(i => i.Cantidad * i.Producto.Precio),
            Estado = "pendiente",
            Detalles = carritoItems.Select(i => new DetallePedido
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.Producto.Precio
            }).ToList()
        };

        _context.Pedidos.Add(pedido);
        _context.CarritoItems.RemoveRange(carritoItems);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pedido creado", pedidoId = pedido.Id });
    }

    // DELETE (admin)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePedido(int id)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "admin")
            return Forbid();

        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null)
            return NotFound();

        _context.Pedidos.Remove(pedido);
        await _context.SaveChangesAsync();

        return NoContent();
    }

 
// 🔥 CAMBIAR ESTADO + EMAIL AUTOMÁTICO PROFESIONAL
[HttpPatch("{id}/estado")]
[Authorize(Roles = "admin")]
public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
{
    var pedido = await _context.Pedidos
        .Include(p => p.Usuario)
            .ThenInclude(u => u.DetalleUsuario)
        .Include(p => p.Detalles)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (pedido == null) return NotFound();
    if (pedido.Estado == nuevoEstado) return Ok(new { mensaje = "El pedido ya tiene ese estado" });

    pedido.Estado = nuevoEstado;
    await _context.SaveChangesAsync();

    // Configuración visual según el estado
    string colorEstado = nuevoEstado switch {
        "Enviado" => "#3b82f6",   // Azul
        "Entregado" => "#10b981",  // Verde
        "Cancelado" => "#ef4444",  // Rojo
        "Pagado" => "#6366f1",     // Indigo
        _ => "#64748b"             // Gris
    };

    string iconoEstado = nuevoEstado switch {
        "Enviado" => "🚚",
        "Entregado" => "✅",
        "Cancelado" => "❌",
        "Pagado" => "💳",
        _ => "📦"
    };

    string nombreCliente = pedido.Usuario.DetalleUsuario?.NombreCompleto ?? pedido.Usuario.NombreUsuario;

    // Cuerpo del Email con diseño de "Timeline" o tarjeta de estado
    string cuerpoHtml = $@"
    <div style='background-color: #f1f5f9; padding: 40px; font-family: sans-serif;'>
        <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05);'>
            
            <div style='background-color: {colorEstado}; padding: 30px; text-align: center; color: white;'>
                <div style='font-size: 50px; margin-bottom: 10px;'>{iconoEstado}</div>
                <h1 style='margin: 0; font-size: 22px; text-transform: uppercase; letter-spacing: 2px;'>Pedido {nuevoEstado}</h1>
            </div>

            <div style='padding: 30px;'>
                <p style='font-size: 16px; color: #1e293b;'>Hola <b>{nombreCliente}</b>,</p>
                <p style='color: #64748b; line-height: 1.6;'>Te informamos que tu pedido <b>#{pedido.Id}</b> ha cambiado su estado a: <span style='color: {colorEstado}; font-weight: bold;'>{nuevoEstado}</span>.</p>
                
                <div style='background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                    <table width='100%' style='font-size: 14px; color: #475569;'>
                        <tr>
                            <td style='padding-bottom: 8px;'><b>Nº de Seguimiento:</b></td>
                            <td style='padding-bottom: 8px; text-align: right;'>{pedido.StripeSessionId?.Substring(0, 10).ToUpper() ?? "N/A"}</td>
                        </tr>
                        <tr>
                            <td style='padding-bottom: 8px;'><b>Total del pedido:</b></td>
                            <td style='padding-bottom: 8px; text-align: right; font-weight: bold; color: #1e293b;'>{pedido.Total:0.00}€</td>
                        </tr>
                        <tr>
                            <td><b>Fecha actualización:</b></td>
                            <td style='text-align: right;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                        </tr>
                    </table>
                </div>

                <div style='text-align: center; margin-top: 30px;'>
                    <a href='https://plantillaecommerce-f5dhckf7acbkd0fe.spaincentral-01.azurewebsites.net/pedidos' style='background-color: {colorEstado}; color: white; padding: 14px 25px; text-decoration: none; border-radius: 10px; font-weight: bold; display: inline-block;'>
                        Ver mi pedido en la web
                    </a>
                </div>
            </div>

            <div style='background: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8;'>
                Si tienes alguna duda sobre tu envío, por favor contacta con soporte.<br>
                © {DateTime.Now.Year} TuTienda Online.
            </div>
        </div>
    </div>";

    _emailService.Send(
        pedido.Usuario.Correo,
        $"{iconoEstado} Actualización de tu pedido #{pedido.Id}",
        cuerpoHtml
    );

    return Ok(new { mensaje = "Estado actualizado y cliente notificado", nuevoEstado });
}
}