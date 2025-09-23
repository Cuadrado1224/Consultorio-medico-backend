namespace Consultorio_Medico_Administracion.Models
{
    public class EmpleadoDTOCrear
    {
        public int Id { get; set; }
        public int centro_medicoID { get; set; }
        public int tipo_empleadoID { get; set; }
        public string nombre { get; set; }
        public string cedula { get; set; }
        public int especialidadID { get; set; }
        public string telefono { get; set; }
        public string email { get; set; }
        public double salario { get; set; }

    }
}
