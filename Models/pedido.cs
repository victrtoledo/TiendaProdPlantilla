
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{public class Pedido
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Estado { get; set; }
    public decimal Total { get; set; }
    public string? StripeSessionId { get; set; } // ← añadir
    public List<DetallePedido> Detalles { get; set; }
    public Usuario? Usuario { get; set; }
}

}
   
