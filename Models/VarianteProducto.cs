namespace TiendaApi.Models
{
    public class VarianteProducto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }            // "5L", "10L", "15L"
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int ProductoId { get; set; }            // producto PADRE (Carbocandy)
        public int ProductoVarianteId { get; set; }  
        public string? ImagenUrl { get; set; }    // producto HIJO (Carbocandy 5L)
        public Producto? Producto { get; set; }
    }
}