using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VariantesController : ControllerBase
    {
        private readonly TiendaDbContext _context;

        public VariantesController(TiendaDbContext context)
        {
            _context = context;
        }

        [HttpGet("por-producto/{productoId}")]
        public async Task<IActionResult> GetVariantesPorProducto(int productoId)
        {
            var variantes = await _context.VariantesProducto
                .Where(v => v.ProductoId == productoId)
                .ToListAsync();

            return Ok(variantes);
        }

        [HttpPost]
        public async Task<IActionResult> CrearVariante([FromBody] VarianteProducto variante)
        {
            var productoPadre = await _context.Productos.FindAsync(variante.ProductoId);
            if (productoPadre == null) return NotFound("Producto padre no encontrado");

            _context.VariantesProducto.Add(variante);
            await _context.SaveChangesAsync();

            return Ok(variante);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarVariante(int id, [FromBody] VarianteProducto variante)
        {
            if (id != variante.Id) return BadRequest();

            _context.Entry(variante).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(variante);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarVariante(int id)
        {
            var variante = await _context.VariantesProducto.FindAsync(id);
            if (variante == null) return NotFound();

            _context.VariantesProducto.Remove(variante);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}