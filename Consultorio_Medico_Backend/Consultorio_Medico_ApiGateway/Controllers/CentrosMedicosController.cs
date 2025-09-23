using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Consulltorio_Medico_Autenticacion.Protos;
using Consulltorio_Medico_Administracion.Protos;
using Consulltorio_Medico_Administracion.Administracion;

namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("Administracion")]
    [ApiController]
    public class CentrosMedicosController : ControllerBase
    {
        IConfiguration _configuration;
        public CentrosMedicosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // GET: api/Centro_Medico
        [HttpGet("CentrosMedicos")]
        public async Task<ActionResult<Centro_MedicoLista>> GetCentros_Medicos()
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

                var centrosLista = await cliente.GetAllCentro_MedicoAsync(new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { }, callOptionsToken());

                return centrosLista;
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

        // GET: api/Centro_Medico/5
        [HttpGet("CentrosMedicos/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Centro_Medico>> GetCentro_Medico(int id)
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

                var CentroMedico = await cliente.GetCentro_MedicoAsync(new Centro_MedicoGet { Id = id }, callOptionsToken());

                return CentroMedico;
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

        // PUT: api/Centro_Medico/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("CentrosMedicos/{id}")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Centro_Medico>> PutCentro_Medico(int id, Consulltorio_Medico_Administracion.Administracion.Centro_Medico centro_Medico)
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

                var CentroModificado = await cliente.PutCentro_MedicoAsync(centro_Medico, callOptionsToken());

                return CentroModificado;
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

        // POST: api/Centro_Medico
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("CentrosMedicos")]
        public async Task<ActionResult<Consulltorio_Medico_Administracion.Administracion.Centro_Medico>> PostCentro_Medico(Centro_MedicoPost centro_Medico)
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

                var centroMedico = await cliente.PostCentro_MedicoAsync(centro_Medico, callOptionsToken());

                return centroMedico;
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

        // DELETE: api/Centro_Medico/5
        [HttpDelete("CentrosMedicos/{id}")]
        public async Task<IActionResult> DeleteCentro_Medico(int id)
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

                var empleadoLista = await cliente.DeleteCentro_MedicoAsync(new Centro_MedicoGet { Id = id }, callOptionsToken());

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





