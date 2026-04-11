
namespace TiendaApi.Dtos
{

    public class CrearSesionDto
    {
        public int UsuarioId { get; set; }
        public List<ProductoDto> Productos { get; set; } = new();
    }

}