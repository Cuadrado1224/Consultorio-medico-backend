using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

using Consulltorio_Medico_Consultas.Protos;
using Consulltorio_Medico_Administracion.Administracion;


namespace Consultorio_Medico_ApiGateway.Controllers
{
    [Route("CentroMedico/Pacientes")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        IConfiguration _configuration;
        public PacientesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<GetPacienteListaResponse>> GetPacientes()
        {

            try
            {
                var token = Request.Headers["Authorization"].ToString();
                var payload = leerPayload(token);

                if (payload == null || !payload.TryGetValue("TipoEmpleado", out var tipoEmpleado))
                {
                    return BadRequest("Token inválido o sin tipo de empleado.");
                }

                if (tipoEmpleado.ToString() == "Administrador")
                {
                    var pacientes = new List<PacienteModel>();
                    var centros = _configuration.GetSection("grcp:centrosMedicos").Get<List<string>>();

                    foreach (var clave in centros)
                    {
                        var url = _configuration[$"grcp:{clave}"];
                        if (string.IsNullOrEmpty(url)) continue;

                        using var canal = GrpcChannel.ForAddress(url, new GrpcChannelOptions
                        {
                            HttpHandler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            }
                        });

                        var cliente = new PacienteService.PacienteServiceClient(canal);
                        try
                        {
                            var respuesta = await cliente.GetAllPacienteAsync(new EmptyResponse { }, callOptionsToken());
                            if (respuesta?.Pacientes != null)
                            {
                                pacientes.AddRange(respuesta.Pacientes);
                            }
                        }
                        catch { continue; }
                    }

                    if (pacientes.Count == 0)
                        return NotFound("Paciente no encontrado.");

                    return Ok(pacientes);
                }

                // Si NO es administrador, consultar solo su centro
                if (!payload.TryGetValue("CentroMedico", out var centroMedico))
                {
                    return BadRequest("No se pudo determinar el centro médico.");
                }

                var claveCentro = $"centroMedico-{centroMedico}";
                var urlCentro = _configuration[$"grcp:{claveCentro}"];
                if (string.IsNullOrEmpty(urlCentro))
                {
                    return BadRequest("Centro médico no configurado.");
                }

                var httpHandlerSolo = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var canalSolo = GrpcChannel.ForAddress(urlCentro, new GrpcChannelOptions { HttpHandler = httpHandlerSolo });
                var clienteSolo = new PacienteService.PacienteServiceClient(canalSolo);

                var pacientesCiudad = await clienteSolo.GetAllPacienteAsync(new EmptyResponse { }, callOptionsToken());

                if (pacientesCiudad == null)
                {
                    return NotFound("Paciente no encontrado.");
                }
                return pacientesCiudad;

            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (Exception)
            {
                return BadRequest("Error general.");
            }


        }


        [HttpGet("{id}")]
        public async Task<ActionResult<GetPacienteListaResponse>> GetPaciente(int id)
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString();
                var payload = leerPayload(token);

                if (payload == null || !payload.TryGetValue("TipoEmpleado", out var tipoEmpleado))
                {
                    return BadRequest("Token inválido o sin tipo de empleado.");
                }

                if (tipoEmpleado.ToString() == "Administrador")
                {
                    var pacientes = new List<PacienteModel>();
                    var centros = _configuration.GetSection("grcp:centrosMedicos").Get<List<string>>();

                    foreach (var clave in centros)
                    {
                        var url = _configuration[$"grcp:{clave}"];
                        if (string.IsNullOrEmpty(url)) continue;

                        using var canal = GrpcChannel.ForAddress(url, new GrpcChannelOptions
                        {
                            HttpHandler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            }
                        });

                        var cliente = new PacienteService.PacienteServiceClient(canal);
                        try
                        {
                            var respuesta = await cliente.GetPacienteAsync(new GetPacienteRequest { IdPaciente = id }, callOptionsToken());
                            if (respuesta?.Paciente != null)
                            {
                                pacientes.Add(respuesta.Paciente);
                            }
                        }
                        catch { continue; }
                    }

                    if (pacientes.Count == 0)
                        return NotFound("Paciente no encontrado.");

                    return Ok(pacientes);
                }

                // Si NO es administrador, consultar solo su centro
                if (!payload.TryGetValue("CentroMedico", out var centroMedico))
                {
                    return BadRequest("No se pudo determinar el centro médico.");
                }

                var claveCentro = $"centroMedico-{centroMedico}";
                var urlCentro = _configuration[$"grcp:{claveCentro}"];
                if (string.IsNullOrEmpty(urlCentro))
                {
                    return BadRequest("Centro médico no configurado.");
                }

                var httpHandlerSolo = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var canalSolo = GrpcChannel.ForAddress(urlCentro, new GrpcChannelOptions { HttpHandler = httpHandlerSolo });
                var clienteSolo = new PacienteService.PacienteServiceClient(canalSolo);

                var pacienteSolo = await clienteSolo.GetPacienteAsync(new GetPacienteRequest { IdPaciente = id }, callOptionsToken());

                if (pacienteSolo == null)
                {
                    return NotFound("Paciente no encontrado.");
                }
                return new GetPacienteListaResponse { Pacientes = { pacienteSolo.Paciente } };

            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
            catch (Exception)
            {
                return BadRequest("Error general.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<PacienteModel>> PostPaciente(CrearPacienteRequest paciente)
        {
            try
            {
                var ciudad = this.GetCentro_Medico(paciente.IdCentroMedico).Result.Ciudad;

                var url = _configuration[$"grcp:centroMedico-{ciudad}"];

                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var canal = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = httpHandler });
                var cliente = new PacienteService.PacienteServiceClient(canal);

                var respuesta = await cliente.CrearPacienteAsync(paciente, callOptionsToken());
                return respuesta.Paciente;
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PacienteModel>> PutPaciente(int id, ActualizarPacienteRequest paciente)
        {
            try
            {
                var ciudad = this.GetCentro_Medico(paciente.IdCentroMedico).Result.Ciudad;

                var url = _configuration[$"grcp:centroMedico-{ciudad}"];

                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var canal = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = httpHandler });
                var cliente = new PacienteService.PacienteServiceClient(canal);

                var respuesta = await cliente.ActualizarPacienteAsync(paciente, callOptionsToken());
                return respuesta.Paciente;
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaciente(int id, CrearPacienteRequest paciente)
        {
            try
            {
                var ciudad = this.GetCentro_Medico(paciente.IdCentroMedico).Result.Ciudad;

                var url = _configuration[$"grcp:centroMedico-{ciudad}"];

                var httpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var canal = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = httpHandler });
                var cliente = new PacienteService.PacienteServiceClient(canal);

                var respuesta = await cliente.EliminarPacienteAsync(new EliminarPacienteRequest { IdPaciente = paciente.IdPaciente }, callOptionsToken());
                return Ok();
            }
            catch (RpcException ex)
            {
                return erroresGrpc(ex.StatusCode, ex.Status.Detail);
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

        private IDictionary<string, object> leerPayload(string token)
        {

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length).Trim();
            }
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Payload;
        }


        private async Task<Centro_Medico> GetCentro_Medico(int id_centro_medico)
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

            var centro_Medico = await cliente.GetCentro_MedicoAsync(new Centro_MedicoGet { Id = id_centro_medico }, this.callOptionsToken());

            if (centro_Medico == null)
            {
                throw new RpcException(new Status(Grpc.Core.StatusCode.NotFound, "Centro Medico no encontrado"));
            }

            return centro_Medico;
        }

    }
}



