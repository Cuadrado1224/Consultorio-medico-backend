using System.ComponentModel.DataAnnotations;

namespace Consulltorio_Medico_Consultas.Models
{
    public class ConsultasMedicasEntity
    {
        [Key]
        public int id_consulta_medica { set; get; }
        public DateOnly fecha { set; get; }
        public string hora { set; get; }
        public string motivo { set; get; }
        public string diagnostico { set; get; }
        public string tratamiento { set; get; }
        public int id_empleado { get; set; }
        public Paciente paciente { set; get; }

        public int id_centro_medico { get; set; }

        // Constructor personalizado
        public ConsultasMedicasEntity(DateOnly fecha, string hora, string motivo, string diagnostico, string tratamiento, int id_empleado, Paciente paciente, int id_centroMedico)
        {
            this.fecha = fecha;
            this.hora = hora;
            this.motivo = motivo;
            this.diagnostico = diagnostico;
            this.tratamiento = tratamiento;
            this.id_empleado = id_empleado;
            this.paciente = paciente;
            this.id_centro_medico = id_centroMedico;
        }

        // Constructor sin parámetros (requerido por Entity Framework)
        public ConsultasMedicasEntity()
        {
        }
    }
}
