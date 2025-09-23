using Consulltorio_Medico_Administracion.Administracion;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Consulltorio_Medico_Administracion.Protos;
using Consulltorio_Medico_Autenticacion.Protos;


namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("Administracion")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        IConfiguration _configuration;
        public EmpleadosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Empleados")]
        public async Task<ActionResult<EmpleadoLista>> GetEmpleados()
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

                var empleadoLista = await cliente.GetAllEmpleadoAsync(new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { }, callOptionsToken());

                return empleadoLista;
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
        [HttpGet("Empleados/Especialidad/{id}")]
        public async Task<ActionResult<EmpleadoLista>> GetEmpleadosByEspecialidad(int id)
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

                var empleadoLista = await cliente.GetAllEmpleadoByEspecialidadAsync(new EspecialidadGet { Id = id }, callOptionsToken());

                return empleadoLista;
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
        [HttpGet("Empleados/CentroMedico/{id}")]
        public async Task<ActionResult<EmpleadoLista>> GetEmpleadosByCentroMedico(int id)
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

                var empleadoLista = await cliente.GetAllEmpleadoByCentroMedicoAsync(new Centro_MedicoGet { Id = id }, callOptionsToken());

                return empleadoLista;
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
        // GET: api/Empleadoes/5
        [HttpGet("Empleados/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Empleado>> GetEmpleado(int id)
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

                var empleado = await cliente.GetEmpleadoAsync(new EmpleadoGet { Id = id }, callOptionsToken());

                return empleado;
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

        // PUT: api/Empleadoes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("Empleados/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Empleado>> PutEmpleado(int id, EmpleadoPut empleado)
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

                var empleadoLista = await cliente.PutEmpleadoAsync(empleado, callOptionsToken());

                return empleadoLista;
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

        // POST: api/Empleadoes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Empleados")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Empleado>> PostEmpleado(EmpleadoPost empleado)
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

                var empleadoLista = await cliente.PostEmpleadoAsync(empleado, callOptionsToken());

                return empleadoLista;
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

        // DELETE: api/Empleadoes/5
        [HttpDelete("Empleados/{id}")]
        public async Task<IActionResult> DeleteEmpleado(int id)
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

                var empleadoLista = await cliente.DeleteEmpleadoAsync(new EmpleadoGet { Id = id }, callOptionsToken());

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

    

