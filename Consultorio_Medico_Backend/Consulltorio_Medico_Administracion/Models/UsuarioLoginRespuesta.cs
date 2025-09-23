using System.Text.Json.Serialization;

namespace Consultorio_Medico_Administracion.Models
{
    public class UsuarioLoginRespuesta
    {
        public bool EsValido { get; set; }
        public Usuario Usuario { get; set; }
    }
}
