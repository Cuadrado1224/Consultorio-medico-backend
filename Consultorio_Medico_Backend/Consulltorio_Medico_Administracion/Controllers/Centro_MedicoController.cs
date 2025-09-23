
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
    public class Centro_MedicoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public Centro_MedicoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Centro_Medico
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Centro_Medico>>> GetCentros_Medicos()
        {
            return await _context.Centros_Medicos.ToListAsync();
        }

        // GET: api/Centro_Medico/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Centro_Medico>> GetCentro_Medico(int id)
        {
            var centro_Medico = await _context.Centros_Medicos.FindAsync(id);

            if (centro_Medico == null)
            {
                return NotFound();
            }

            return centro_Medico;
        }

        // PUT: api/Centro_Medico/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCentro_Medico(int id, Centro_Medico centro_Medico)
        {
            if (id != centro_Medico.Id)
            {
                return BadRequest();
            }

            _context.Entry(centro_Medico).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Centro_MedicoExists(id))
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

        // POST: api/Centro_Medico
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Centro_Medico>> PostCentro_Medico(Centro_Medico centro_Medico)
        {
            _context.Centros_Medicos.Add(centro_Medico);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCentro_Medico", new { id = centro_Medico.Id }, centro_Medico);
        }

        // DELETE: api/Centro_Medico/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCentro_Medico(int id)
        {
            var centro_Medico = await _context.Centros_Medicos.FindAsync(id);
            if (centro_Medico == null)
            {
                return NotFound();
            }

            _context.Centros_Medicos.Remove(centro_Medico);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Centro_MedicoExists(int id)
        {
            return _context.Centros_Medicos.Any(e => e.Id == id);
        }
    }
}
