using Consultorio_Medico_Administracion.Data;
using Consultorio_Medico_Administracion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Consultorio_Medico_Administracion.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "TipoEmpleadoPolitica")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmpleadosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Empleadoes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmpleadoDTOSeleccionar>>> GetEmpleados()
        {
            var empleados = await _context.Empleados
                .Include(e=>e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .ToListAsync();
            var empleadosdto = empleados.Select(
                e=> new EmpleadoDTOSeleccionar
                {
                    Id = e.Id,
                    cedula = e.cedula,
                    email = e.email,
                    nombre = e.nombre,
                    telefono = e.telefono,
                    Centro_Medico=new Centro_Medico { 
                        nombre=e.Centro_Medico.nombre,
                        Id=e.Centro_Medico.Id,
                        ciudad=e.Centro_Medico.ciudad,
                        direccion = e.Centro_Medico.direccion
                    },
                    Especialidad=new Especialidad
                    {
                        Id=e.especialidadID,
                        especialidad=e.Especialidad.especialidad
                    },
                    Tipo_Empleado=new Tipo_Empleado { 
                        Id=e.tipo_empleadoID,
                        tipo=e.Tipo_Empleado.tipo
                    },
                    salario = e.salario
                }
                );
            return Ok(empleadosdto);
        }

        // GET: api/Empleadoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmpleadoDTOSeleccionar>> GetEmpleado(int id)
        {
            var e = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .FirstOrDefaultAsync(e=>e.Id==id);

            if (e == null)
            {
                return NotFound();
            }

            var empleadoDTO = new EmpleadoDTOSeleccionar
            {
                Id = e.Id,
                cedula = e.cedula,
                email = e.email,
                nombre = e.nombre,
                telefono = e.telefono,
                Centro_Medico = new Centro_Medico
                {
                    nombre = e.Centro_Medico.nombre,
                    Id = e.Centro_Medico.Id,
                    ciudad = e.Centro_Medico.ciudad,
                    direccion = e.Centro_Medico.direccion
                },
                Especialidad = new Especialidad
                {
                    Id = e.especialidadID,
                    especialidad = e.Especialidad.especialidad
                },
                Tipo_Empleado = new Tipo_Empleado
                {
                    Id = e.tipo_empleadoID,
                    tipo = e.Tipo_Empleado.tipo
                },
                salario = e.salario
            };

            return empleadoDTO;
        }

        // PUT: api/Empleadoes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpleado(int id, EmpleadoDTOCrear empleado)
        {
            if (id != empleado.Id)
            {
                return BadRequest();
            }

            var empleadoExistente = await _context.Empleados.FindAsync(id);

            if (empleadoExistente == null)
            {
                return NotFound();
            }

            // Mapea los datos del DTO a la entidad
            empleadoExistente.cedula = empleado.cedula;
            empleadoExistente.email = empleado.email;
            empleadoExistente.nombre = empleado.nombre;
            empleadoExistente.telefono = empleado.telefono;
            empleadoExistente.salario = empleado.salario;
            empleadoExistente.centro_medicoID = empleado.centro_medicoID;
            empleadoExistente.especialidadID = empleado.especialidadID;
            empleadoExistente.tipo_empleadoID = empleado.tipo_empleadoID;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpleadoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Empleadoes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<EmpleadoDTOSeleccionar>> PostEmpleado(EmpleadoDTOCrear empleado)
        {
            var empleadocrear = new Empleado
            {
                Id = empleado.Id,
                cedula = empleado.cedula,
                email = empleado.email,
                nombre = empleado.nombre,
                telefono = empleado.telefono,
                salario = empleado.salario,
                centro_medicoID = empleado.centro_medicoID,
                especialidadID = empleado.especialidadID,
                tipo_empleadoID = empleado.tipo_empleadoID
            };
            _context.Empleados.Add(empleadocrear);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmpleado", new { id = empleado.Id }, empleado);
        }

        // DELETE: api/Empleadoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpleado(int id)
        {

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            _context.Empleados.Remove(empleado);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.Id == id);
        }
    }
}
