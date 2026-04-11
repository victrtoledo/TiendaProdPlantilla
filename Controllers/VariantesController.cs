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

            // Crear producto hijo automáticamente
            var productoHijo = new Producto
            {
                Nombre = $"{productoPadre.Nombre} {variante.Nombre}",
                Descripcion = productoPadre.Descripcion,
                Precio = variante.Precio,
                Stock = variante.Stock,
                ImagenUrl = productoPadre.ImagenUrl,
                CategoriaId = productoPadre.CategoriaId
            };

            _context.Productos.Add(productoHijo);
            await _context.SaveChangesAsync();

            // Guardar variante apuntando al producto hijo
            variante.ProductoVarianteId = productoHijo.Id;
            _context.VariantesProducto.Add(variante);
            await _context.SaveChangesAsync();

            return Ok(new { variante, productoHijo });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarVariante(int id, [FromBody] VarianteProducto variante)
        {
            if (id != variante.Id) return BadRequest();

            // Actualizar también el producto hijo
            var productoHijo = await _context.Productos.FindAsync(variante.ProductoVarianteId);
            if (productoHijo != null)
            {
                var productoPadre = await _context.Productos.FindAsync(variante.ProductoId);
                productoHijo.Nombre = $"{productoPadre!.Nombre} {variante.Nombre}";
                productoHijo.Precio = variante.Precio;
                productoHijo.Stock = variante.Stock;
            }

            _context.Entry(variante).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(variante);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarVariante(int id)
        {
            var variante = await _context.VariantesProducto.FindAsync(id);
            if (variante == null) return NotFound();

            // Eliminar también el producto hijo
            var productoHijo = await _context.Productos.FindAsync(variante.ProductoVarianteId);
            if (productoHijo != null)
                _context.Productos.Remove(productoHijo);

            _context.VariantesProducto.Remove(variante);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}