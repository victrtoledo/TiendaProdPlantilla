
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Models
{
    // Models/DetalleUsuario.cs
public class DetalleUsuario
{
    public int Id { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Ciudad { get; set; }
    public string? CodigoPostal { get; set; }

    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
   
}