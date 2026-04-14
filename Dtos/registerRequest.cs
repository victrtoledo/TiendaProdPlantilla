
namespace TiendaApi.Dtos
{

    public class RegisterRequest
{
    public string NombreUsuario { get; set; }
    public string Correo { get; set; }
    public string Contrasena { get; set; }
    public string Rol { get; set; }
    public string TipoUsuario { get; set; }

    // Estos deben coincidir con lo que envía Angular
    public string? NombreEmpresa { get; set; }
    public string? Cif { get; set; }
    public string? Telefono { get; set; }
}
}