
using Consulltorio_Medico_Administracion.Data;
using Consulltorio_Medico_Administracion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Consultorio_Medico_Administracion.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "TipoEmpleadoPolitica")]
    [ApiController]
    public class Tipo_EmpleadoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public Tipo_EmpleadoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Tipo_Empleado
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tipo_Empleado>>> GetTipos_Empleados()
        {
            return await _context.Tipos_Empleados.ToListAsync();
        }

        // GET: api/Tipo_Empleado/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tipo_Empleado>> GetTipo_Empleado(int id)
        {
            var tipo_Empleado = await _context.Tipos_Empleados.FindAsync(id);

            if (tipo_Empleado == null)
            {
                return NotFound();
            }

            return tipo_Empleado;
        }

        // PUT: api/Tipo_Empleado/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTipo_Empleado(int id, Tipo_Empleado tipo_Empleado)
        {
            if (id != tipo_Empleado.Id)
            {
                return BadRequest();
            }

            _context.Entry(tipo_Empleado).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Tipo_EmpleadoExists(id))
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

        // POST: api/Tipo_Empleado
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Tipo_Empleado>> PostTipo_Empleado(Tipo_Empleado tipo_Empleado)
        {
            _context.Tipos_Empleados.Add(tipo_Empleado);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTipo_Empleado", new { id = tipo_Empleado.Id }, tipo_Empleado);
        }

        // DELETE: api/Tipo_Empleado/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTipo_Empleado(int id)
        {
            var tipo_Empleado = await _context.Tipos_Empleados.FindAsync(id);
            if (tipo_Empleado == null)
            {
                return NotFound();
            }

            _context.Tipos_Empleados.Remove(tipo_Empleado);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Tipo_EmpleadoExists(int id)
        {
            return _context.Tipos_Empleados.Any(e => e.Id == id);
        }
    }
}
