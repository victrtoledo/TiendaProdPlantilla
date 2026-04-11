
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{
    public class DetallePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
        public int? VarianteId { get; set; }           // ← nuevo
        public VarianteProducto? Variante { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    

}
   
}