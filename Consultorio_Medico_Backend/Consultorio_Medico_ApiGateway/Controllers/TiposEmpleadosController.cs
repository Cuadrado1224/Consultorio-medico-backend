using Consulltorio_Medico_Administracion.Administracion;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("Administracion")]
    [ApiController]
    public class TiposEmpleadosController : ControllerBase
    {
        IConfiguration _configuration;
        public TiposEmpleadosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/Tipo_Empleado
        [HttpGet("TiposEmpleados")]
        public async Task<ActionResult<Tipo_EmpleadoLista>> GetTipos_Empleados()
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

                var tiposLista = await cliente.GetAllTipo_EmpleadoAsync(new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { }, callOptionsToken());

                return tiposLista;
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

        // GET: api/Tipo_Empleado/5
        [HttpGet("TiposEmpleados/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado>> GetTipo_Empleado(int id)
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

                var tipo = await cliente.GetTipo_EmpleadoAsync(new Tipo_EmpleadoGet { Id = id }, callOptionsToken());

                return tipo;
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

        // PUT: api/Tipo_Empleado/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("TiposEmpleados/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado>> PutTipo_Empleado(int id, Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado tipo_Empleado)
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

                var tipoModificado = await cliente.PutTipo_EmpleadoAsync(tipo_Empleado, callOptionsToken());

                return tipoModificado;
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

        // POST: api/Tipo_Empleado
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("TiposEmpleados")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado>> PostTipo_Empleado(Tipo_EmpleadoPost tipo_Empleado)
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

                var tipo = await cliente.PostTipo_EmpleadoAsync(tipo_Empleado, callOptionsToken());

                return tipo;
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

        // DELETE: api/Tipo_Empleado/5
        [HttpDelete("TiposEmpleados/{id}")]
        public async Task<IActionResult> DeleteTipo_Empleado(int id)
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

                var especialidadesLista = await cliente.DeleteTipo_EmpleadoAsync(new Tipo_EmpleadoGet { Id = id }, callOptionsToken());

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

    

