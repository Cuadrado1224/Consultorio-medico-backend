using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Consulltorio_Medico_Administracion.Protos;

namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("Administracion")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        IConfiguration _configuration;
        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // GET: api/Usuarios
        [HttpGet("Usuarios")]
        public async Task<ActionResult<ListaUsuarios>> GetUsuarios()
        {
            try
            {
                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var canal = GrpcChannel.ForAddress(_configuration["grcp:administracion"], new GrpcChannelOptions
                {
                    HttpHandler = httpHandler
                });
                var cliente = new UsuarioService.UsuarioServiceClient(canal);

                var usuariosLista = await cliente.SeleccionarUsuariosAsync(new RespuestaVacia { }, callOptionsToken());

                return usuariosLista;
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest("Sin Token");
            }
        }

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Usuarios")]
        public async Task<ActionResult<Usuario>> PostUsuario(UsuarioRegistro usuario)
        {
            try
            {
                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var canal = GrpcChannel.ForAddress(_configuration["grcp:administracion"], new GrpcChannelOptions
                {
                    HttpHandler = httpHandler
                });
                var cliente = new UsuarioService.UsuarioServiceClient(canal);

                var usuarioRegistro = await cliente.RegistrarUsuarioAsync(usuario, callOptionsToken());

                return usuarioRegistro;
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest("Sin Token");
            }
        }

        [HttpPut("Usuarios/{id}")]
        public async Task<ActionResult<Usuario>> PutUsuario(int id, UsuarioActualizar usuario)
        {
            try
            {
                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var canal = GrpcChannel.ForAddress(_configuration["grcp:administracion"], new GrpcChannelOptions
                {
                    HttpHandler = httpHandler
                });
                var cliente = new UsuarioService.UsuarioServiceClient(canal);

                var usuarioRegistro = await cliente.ActualizarUsuarioAsync(usuario, callOptionsToken());

                return usuarioRegistro;
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest("Sin Token");
            }
        }


        // DELETE: api/Usuarios/5
        [HttpDelete("Usuarios/{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var canal = GrpcChannel.ForAddress(_configuration["grcp:administracion"], new GrpcChannelOptions
                {
                    HttpHandler = httpHandler
                });
                var cliente = new UsuarioService.UsuarioServiceClient(canal);

                var usuariosLista = await cliente.BorrarUsuarioAsync(new UsuarioBorrar { Id = id }, callOptionsToken());

                return Ok();
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest("Sin Token");
            }
        }



        private CallOptions callOptionsToken()
        {
            var token = Request.Headers["Authorization"];
            var metadata = new Metadata {
                { "Authorization",token}
            };
            return new CallOptions
            (
                headers: metadata
            );
        }
        private ObjectResult erroresGrpc(Grpc.Core.StatusCode codigo, string mensaje)
        {
            switch (codigo)
            {
                case Grpc.Core.StatusCode.InvalidArgument:
                    return BadRequest(new { mensaje = mensaje });

                case Grpc.Core.StatusCode.Unauthenticated:
                    return Unauthorized(new { mensaje = mensaje });

                case Grpc.Core.StatusCode.NotFound:
                    return NotFound(new { mensaje = mensaje });

                case Grpc.Core.StatusCode.AlreadyExists:
                    return Conflict(new { mensaje = mensaje });

                default:
                    return StatusCode(500, new { mensaje = "Error interno: " + mensaje });
            }
        }
    }
}




