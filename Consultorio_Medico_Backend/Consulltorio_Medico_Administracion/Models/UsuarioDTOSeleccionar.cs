using System.Text.Json.Serialization;

namespace Consulltorio_Medico_Administracion.Models
{
    public class UsuarioDTOSeleccionar
    {
        public int Id { get; set; }
        public string nombre_usuario { get; set; }
        public Empleado empleado { get; set; }
    }
}
