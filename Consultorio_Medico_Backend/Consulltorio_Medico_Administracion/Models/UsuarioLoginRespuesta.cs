using System.Text.Json.Serialization;

namespace Microservicio_Administracion.Models
{
    public class UsuarioLoginRespuesta
    {
        public bool EsValido { get; set; }
        public Usuario Usuario { get; set; }
    }
}
