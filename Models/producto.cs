
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }
         public string? DescripcionLarga { get; set; }

        [Required]
        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public string? ImagenUrl { get; set; }

        // Relación con Categoria
        public int CategoriaId { get; set; }

        
        public Categoria? Categoria { get; set; }
        
        public List<VarianteProducto>? Variantes { get; set; }

}
   
}