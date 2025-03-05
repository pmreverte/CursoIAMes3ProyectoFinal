using Microsoft.AspNetCore.Identity;
using System;

namespace Sprint2.Models
{
    /// <summary>
    /// Representa un usuario de la aplicación con propiedades adicionales.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Obtiene o establece el nombre del usuario.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el apellido del usuario.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el nombre completo del usuario.
        /// </summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece la fecha de registro del usuario.
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Obtiene o establece la última fecha de acceso del usuario.
        /// </summary>
        public DateTime? UltimoAcceso { get; set; }

        /// <summary>
        /// Obtiene o establece si el usuario ha aceptado los términos y condiciones.
        /// </summary>
        public bool AceptoTerminos { get; set; }
    }
}
