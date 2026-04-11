namespace TiendaApi.Models
{
    public class VarianteProducto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? ImagenUrl { get; set; } // ← nueva
        public int ProductoId { get; set; }
        public int ProductoVarianteId { get; set; }
        public Producto? Producto { get; set; }
    }
}