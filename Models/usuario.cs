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
        public string TipoUsuario { get; set; } = "cliente"; // "cliente" o "profesional"

        [Required]
        public string Rol { get; set; } = "cliente"; // "cliente" o "admin"

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // --- CAMPOS PARA PROFESIONALES ---
        
        /// <summary>
        /// Define si el usuario puede hacer login. 
        /// Los clientes normales serán 'true' por defecto.
        /// Los profesionales serán 'false' hasta que el admin los valide.
        /// </summary>
        public bool Activo { get; set; } = true;

        [StringLength(150)]
        public string? NombreEmpresa { get; set; }

        [StringLength(20)]
        public string? Cif { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        // Relación existente
        public DetalleUsuario? DetalleUsuario { get; set; }
    }
}