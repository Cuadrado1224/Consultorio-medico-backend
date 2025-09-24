using Consulltorio_Medico_Administracion.Administracion;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("Administracion")]
    [ApiController]
    public class EspecialidadesController : ControllerBase
    {
        IConfiguration _configuration;
        public EspecialidadesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // GET: api/Especialidads
        [HttpGet("Especialidades")]
        public async Task<ActionResult<EspecialidadLista>> GetEspecialidades()
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

                var cliente = new AdministracionService.AdministracionServiceClient(canal);

                var especialidadesLista = await cliente.GetAllEspecialidadesAsync(new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { }, callOptionsToken());

                return especialidadesLista;
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

        // GET: api/Especialidads/5
        [HttpGet("Especialidades/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Especialidad>> GetEspecialidad(int id)
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
                var cliente = new AdministracionService.AdministracionServiceClient(canal);

                var especialidad = await cliente.GetEspecialidadesAsync(new EspecialidadGet { Id = id }, callOptionsToken());

                return especialidad;
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

        // PUT: api/Especialidads/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("Especialidades/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Especialidad>> PutEspecialidad(int id, Consulltorio_Medico_Administracion.Administracion.Especialidad especialidad)
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
                var cliente = new AdministracionService.AdministracionServiceClient(canal);

                var especialidadModificar = await cliente.PutEspecialidadAsync(especialidad, callOptionsToken());

                return especialidadModificar;
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

        // POST: api/Especialidads
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Especialidades")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Especialidad>> PostEspecialidad(EspecialidadPost especialidad)
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
                var cliente = new AdministracionService.AdministracionServiceClient(canal);

                var especialidadRespuesta = await cliente.PostEspecialidadAsync(especialidad, callOptionsToken());

                return especialidadRespuesta;
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

        // DELETE: api/Especialidads/5
        [HttpDelete("Especialidades/{id}")]
        public async Task<IActionResult> DeleteEspecialidad(int id)
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
                var cliente = new AdministracionService.AdministracionServiceClient(canal);

                var centrosLista = await cliente.DeleteEspecialidadAsync(new EspecialidadGet { Id = id }, callOptionsToken());

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




