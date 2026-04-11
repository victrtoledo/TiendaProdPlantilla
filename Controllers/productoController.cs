using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly TiendaDbContext _context;

        public ProductosController(TiendaDbContext context)
        {
            _context = context;
        }

        // GET: api/productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos
                        .Include(p => p.Categoria)
                        .ToListAsync();
        }

        // GET: api/productos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _context.Productos
                                .Include(p => p.Categoria)
                                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
                return NotFound();

            return producto;
        }

        // POST: api/productos
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
        }

        // PUT: api/productos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if (id != producto.Id)
                return BadRequest();

            _context.Entry(producto).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("relacionados/{categoriaId}/{productoId}")]
        public async Task<IActionResult> GetRelacionados(int categoriaId, int productoId)
        {
            var productos = await _context.Productos
                .Where(p => p.CategoriaId == categoriaId && p.Id != productoId)
                .Take(4)
                .ToListAsync();

            return Ok(productos);
        }

        // DELETE: api/productos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
         // GET: api/productos/por-categoria/1
        [HttpGet("por-categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductosPorCategoria(int categoriaId)
        {
            var productos = await _context.Productos
                .Where(p => p.CategoriaId == categoriaId)
                .ToListAsync();

            return Ok(productos);
        }
    }

    
}
