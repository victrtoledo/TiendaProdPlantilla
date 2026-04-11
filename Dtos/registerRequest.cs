
namespace TiendaApi.Dtos
{

    public class RegisterRequest
    {
        public string NombreUsuario { get; set; }
        public string Correo { get; set; }
        public string Contrasena { get; set; }
        public string Rol { get; set; } = "cliente";
    }
}