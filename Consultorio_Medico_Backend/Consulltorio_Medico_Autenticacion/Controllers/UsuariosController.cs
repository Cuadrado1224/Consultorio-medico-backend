
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grpc.Net.Client;
using Consulltorio_Medico_Administracion.Protos;
using Consulltorio_Medico_Autenticacion.Auth;

namespace Consulltorio_Medico_Autenticacion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UsuariosController(IConfiguration config)
        {
            _config = config;
        }

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<String>> PostUsuario(UsuarioLogin usuario)
        {

            using var canal = GrpcChannel.ForAddress(_config["grcp:administracion"]);

            var cliente = new UsuarioService.UsuarioServiceClient(canal);

            var respuesta = await cliente.ValidarUsuarioAsync(new UsuarioLogin
            {
                NombreUsuario = usuario.NombreUsuario,
                Contrasenia = usuario.Contrasenia
            });

            if (!respuesta.EsValido && respuesta.Usuario == null)
            {
                return BadRequest();
            }
            var TokenProvider = new TokenProvider(_config);

            var Token = TokenProvider.Create(respuesta.Usuario);
            return Token;
        }
    }
}
