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

    // 🔥 CAMBIAR ESTADO + EMAIL AUTOMÁTICO
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound();

        if (pedido.Estado == nuevoEstado)
            return Ok(new { mensaje = "No hay cambios" });

        pedido.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        // 🔥 MENSAJE SEGÚN ESTADO
        string mensaje = nuevoEstado switch
        {
            "Enviado" => "🚚 Tu pedido ha sido enviado",
            "Entregado" => "📦 Tu pedido ha sido entregado",
            "Cancelado" => "❌ Tu pedido ha sido cancelado",
            "Pagado" => "💳 Tu pago ha sido confirmado",
            _ => "📦 Tu pedido ha sido actualizado"
        };

        var cuerpo = $@"
<h2>{mensaje}</h2>

<p><strong>Pedido:</strong> #{pedido.Id}</p>
<p><strong>Estado:</strong> {nuevoEstado}</p>

<p>Gracias por confiar en nosotros 🙌</p>
";

        _emailService.Send(
            pedido.Usuario.Correo,
            "Actualización de tu pedido",
            cuerpo
        );

        return Ok(new { mensaje = "Estado actualizado y notificado", estado = nuevoEstado });
    }
}