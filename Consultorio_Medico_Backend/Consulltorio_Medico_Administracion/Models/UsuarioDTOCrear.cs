using System.Text.Json.Serialization;

namespace Consultorio_Medico_Administracion.Models
{
    public class UsuarioDTOCrear
    {
        public int Id { get; set; }
        public string nombre_usuario { get; set; }
        public string contraseña { get; set; }
        public int empleadoId { get; set; }

    }
}
