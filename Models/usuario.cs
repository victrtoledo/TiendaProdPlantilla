using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{
    public class Usuario
    {
       [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string NombreUsuario { get; set; }

    [Required]
    [EmailAddress]
    public string Correo { get; set; }

    [Required]
    public string ContrasenaHash { get; set; }

    [Required]
    public string Rol { get; set; } = "cliente";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public DetalleUsuario? DetalleUsuario { get; set; }
    }
}