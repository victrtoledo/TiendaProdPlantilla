using Microsoft.EntityFrameworkCore;
using TiendaApi.Models;

namespace TiendaApi.Data
{
    public class TiendaDbContext : DbContext
    {
        public TiendaDbContext(DbContextOptions<TiendaDbContext> options)
            : base(options)
        {

        }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<DetallePedido> DetallePedidos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<CarritoItem> CarritoItems { get; set; }
        public DbSet<DetalleUsuario> DetallesUsuario { get; set; }
        public DbSet<VarianteProducto> VariantesProducto { get; set; }

    }
}