using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TiendaApi.Data;
using TiendaApi.Models;

[ApiController]
[Route("api/[controller]")]
public class CarritoController : ControllerBase
{
    private readonly TiendaDbContext _context;

    public CarritoController(TiendaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarritoItem>>> GetCarrito()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        var carrito = await _context.CarritoItems
            .Where(c => c.UsuarioId == userId)
            .Include(c => c.Producto)
            .Include(c => c.Variante) // ← incluir variante
            .ToListAsync();

        return Ok(carrito);
    }

    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito([FromBody] CarritoItem item)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        // Buscar si ya existe el mismo producto Y variante en el carrito
        var existente = await _context.CarritoItems
            .FirstOrDefaultAsync(c =>
                c.UsuarioId == userId &&
                c.ProductoId == item.ProductoId &&
                c.VarianteId == item.VarianteId); // ← distinguir por variante

        if (existente != null)
        {
            existente.Cantidad += item.Cantidad;
        }
        else
        {
            item.UsuarioId = userId;
            _context.CarritoItems.Add(item);
        }

        await _context.SaveChangesAsync();
        return Ok("Producto agregado al carrito.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarCantidad(int id, [FromBody] int cantidad)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        // Ahora buscamos por id del CarritoItem, no por productoId
        var item = await _context.CarritoItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

        if (item == null)
            return NotFound();

        item.Cantidad = cantidad;
        await _context.SaveChangesAsync();

        return Ok("Cantidad actualizada.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDelCarrito(int id)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        // Ahora buscamos por id del CarritoItem
        var item = await _context.CarritoItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

        if (item == null)
            return NotFound();

        _context.CarritoItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok("Producto eliminado del carrito.");
    }

    [HttpDelete("vaciar")]
    public async Task<IActionResult> VaciarCarrito()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimValue))
            return Unauthorized();

        int userId = int.Parse(claimValue);

        var items = await _context.CarritoItems
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        _context.CarritoItems.RemoveRange(items);
        await _context.SaveChangesAsync();

        return Ok("Carrito vaciado.");
    }
}