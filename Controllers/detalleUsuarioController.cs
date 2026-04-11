using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetalleUsuarioController : ControllerBase
    {
        private readonly TiendaDbContext _context;

        public DetalleUsuarioController(TiendaDbContext context)
        {
            _context = context;
        }

        // GET: api/DetalleUsuario/5
        [HttpGet("{idUsuario}")]
        public async Task<IActionResult> GetDetalle(int idUsuario)
        {
            var detalle = await _context.DetallesUsuario
                .FirstOrDefaultAsync(d => d.UsuarioId == idUsuario);

            if (detalle == null)
                return NotFound();

            return Ok(detalle);
        }

        // POST: api/DetalleUsuario
        [HttpPost]
        public async Task<IActionResult> PostDetalle([FromBody] DetalleUsuario detalle)
        {
            var usuario = await _context.Usuarios.FindAsync(detalle.UsuarioId);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var existente = await _context.DetallesUsuario
                .FirstOrDefaultAsync(d => d.UsuarioId == detalle.UsuarioId);

            if (existente != null)
            {
                // Actualizar datos
                existente.NombreCompleto = detalle.NombreCompleto;
                existente.Direccion = detalle.Direccion;
                existente.Telefono = detalle.Telefono;
                existente.Ciudad = detalle.Ciudad;
                existente.CodigoPostal = detalle.CodigoPostal;

                await _context.SaveChangesAsync();
                return Ok(existente);
            }

            // Nuevo detalle
            _context.DetallesUsuario.Add(detalle);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDetalle), new { idUsuario = detalle.UsuarioId }, detalle);
        }
    }

}