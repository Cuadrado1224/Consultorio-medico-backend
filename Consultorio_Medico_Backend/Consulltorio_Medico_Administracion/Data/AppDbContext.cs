using Microservicio_Administracion.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservicio_Administracion.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { 
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Tipo_Empleado> Tipos_Empleados { get; set; }
        public DbSet<Centro_Medico> Centros_Medicos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.empleado)
                .WithOne()
                .HasForeignKey<Usuario>(u => u.empleadoId);
            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Centro_Medico)               
                .WithMany()                 
                .HasForeignKey(e => e.centro_medicoID);     

            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Especialidad)
                .WithMany()
                .HasForeignKey(e => e.especialidadID);

            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Tipo_Empleado)
                .WithMany()
                .HasForeignKey(e => e.tipo_empleadoID);



        }

    }
}
