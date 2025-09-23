using Consulltorio_Medico_Consultas.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Consulltorio_Medico_Consultas.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<ConsultasMedicasEntity> ConsultasMedicas { get; set; }
        public DbSet<Paciente> Paciente { get; set; }
    }
}
