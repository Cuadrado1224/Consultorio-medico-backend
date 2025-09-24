
using Consulltorio_Medico_Administracion.Data;
using Consulltorio_Medico_Administracion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Consultorio_Medico_Administracion.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy ="TipoEmpleadoPolitica")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDTOSeleccionar>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Include(u=>u.empleado)
                .Include(u => u.empleado.Centro_Medico)
                .Include(u => u.empleado.Tipo_Empleado)
                .Include(u => u.empleado.Especialidad)
                .ToListAsync();
            var dto = usuarios.Select(
                u=> new UsuarioDTOSeleccionar
                {
                    Id = u.Id,
                    nombre_usuario = u.nombre_usuario,
                    empleado = new Empleado
                    {
                        cedula = u.empleado.cedula,
                        email = u.empleado.email,
                        telefono = u.empleado.telefono,
                        nombre = u.empleado.nombre,

                    }
                }
                );
            return Ok(dto);
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDTOSeleccionar>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.empleado)
                .Include(u => u.empleado.Centro_Medico)
                .Include(u => u.empleado.Tipo_Empleado)
                .Include(u => u.empleado.Especialidad)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }
            var dto =new UsuarioDTOSeleccionar { 
            Id = usuario.Id,
            nombre_usuario=usuario.nombre_usuario,
            empleado=new Empleado
            {
                cedula=usuario.empleado.cedula,
                email=usuario.empleado.email,
                telefono=usuario.empleado.telefono,
                nombre = usuario.empleado.nombre,
                
            }
            };
            return dto;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, UsuarioDTOCrear usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest();
            }

            var usuarioModificar = new Usuario
            {
                contraseña = usuario.contraseña,
                Id = usuario.Id,
                empleadoId = usuario.empleadoId,
                nombre_usuario = usuario.nombre_usuario
            };


            _context.Entry(usuarioModificar).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
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

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(UsuarioDTOCrear usuario)
        {
            var usuarioCrear = new Usuario { 
                contraseña=usuario.contraseña,
                Id=usuario.Id,
                empleadoId=usuario.empleadoId,
                nombre_usuario = usuario.nombre_usuario
            };
            _context.Usuarios.Add(usuarioCrear);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UsuarioExists(usuario.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
