using System.Text.Json.Serialization;

namespace Microservicio_Administracion.Models
{
    public class UsuarioDTOSeleccionar
    {
        public int Id { get; set; }
        public string nombre_usuario { get; set; }
        public Empleado empleado { get; set; }
    }
}
