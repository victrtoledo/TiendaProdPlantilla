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

        // GET: api/variantes/por-producto/1
        [HttpGet("por-producto/{productoId}")]
        public async Task<IActionResult> GetVariantesPorProducto(int productoId)
        {
            var variantes = await _context.VariantesProducto
                .Where(v => v.ProductoId == productoId)
                .ToListAsync();

            return Ok(variantes);
        }

        // POST: api/variantes
        [HttpPost]
        public async Task<IActionResult> CrearVariante([FromBody] VarianteProducto variante)
        {
            // Validar producto padre
            var productoPadre = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == variante.ProductoId);

            if (productoPadre == null)
                return NotFound("Producto padre no encontrado");

            // 🔥 SOLO crear variante (NO productos)
            _context.VariantesProducto.Add(variante);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Variante creada correctamente",
                variante
            });
        }

        // PUT: api/variantes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarVariante(int id, [FromBody] VarianteProducto variante)
        {
            if (id != variante.Id)
                return BadRequest("ID no coincide");

            var varianteDb = await _context.VariantesProducto
                .FirstOrDefaultAsync(v => v.Id == id);

            if (varianteDb == null)
                return NotFound("Variante no encontrada");

            // Actualizar campos de variante
            varianteDb.Nombre = variante.Nombre;
            varianteDb.Precio = variante.Precio;
            varianteDb.Stock = variante.Stock;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Variante actualizada",
                varianteDb
            });
        }

        // DELETE: api/variantes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarVariante(int id)
        {
            var variante = await _context.VariantesProducto
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variante == null)
                return NotFound("Variante no encontrada");

            _context.VariantesProducto.Remove(variante);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Variante eliminada"
            });
        }
    }
}