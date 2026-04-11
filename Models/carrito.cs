using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{

    public class CarritoItem
    {
        [Key]
        public int Id { get; set; }

        // Relación con el usuario
        [ForeignKey("Usuario")]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Relación con el producto
        [ForeignKey("Producto")]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int? VarianteId { get; set; }          // ← nuevo, nullable 
        public VarianteProducto? Variante { get; set; }
        public int Cantidad { get; set; }

        
    }


}