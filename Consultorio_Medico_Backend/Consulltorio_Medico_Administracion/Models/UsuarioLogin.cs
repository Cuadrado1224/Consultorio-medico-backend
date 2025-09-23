using System.Text.Json.Serialization;

namespace Microservicio_Administracion.Models
{
    public class UsuarioLogin
    {
        public string nombre_usuario { get; set; }
        public string contraseña { get; set; }
    }
}
