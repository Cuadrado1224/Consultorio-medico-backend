using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microservicio_Administracion.Data;
using Microservicio_Administracion.Models;
using Microsoft.AspNetCore.Authorization;

namespace Microservicio_Administracion.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "TipoEmpleadoPolitica")]
    [ApiController]
    public class EspecialidadesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EspecialidadesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Especialidads
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Especialidad>>> GetEspecialidades()
        {
            return await _context.Especialidades.ToListAsync();
        }

        // GET: api/Especialidads/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Especialidad>> GetEspecialidad(int id)
        {
            var especialidad = await _context.Especialidades.FindAsync(id);

            if (especialidad == null)
            {
                return NotFound();
            }

            return especialidad;
        }

        // PUT: api/Especialidads/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEspecialidad(int id, Especialidad especialidad)
        {
            if (id != especialidad.Id)
            {
                return BadRequest();
            }

            _context.Entry(especialidad).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EspecialidadExists(id))
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

        // POST: api/Especialidads
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Especialidad>> PostEspecialidad(Especialidad especialidad)
        {
            _context.Especialidades.Add(especialidad);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEspecialidad", new { id = especialidad.Id }, especialidad);
        }

        // DELETE: api/Especialidads/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEspecialidad(int id)
        {
            var especialidad = await _context.Especialidades.FindAsync(id);
            if (especialidad == null)
            {
                return NotFound();
            }

            _context.Especialidades.Remove(especialidad);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EspecialidadExists(int id)
        {
            return _context.Especialidades.Any(e => e.Id == id);
        }
    }
}
