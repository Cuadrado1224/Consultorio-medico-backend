using Consulltorio_Medico_Consultas.Data;
using Consulltorio_Medico_Consultas.Models;
using Consulltorio_Medico_Consultas.Protos;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Servicio_Administracion.Administracion;

namespace Consulltorio_Medico_Consultas.protos
{
    public class PacienteServiceImpl : PacienteService.PacienteServiceBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        public PacienteServiceImpl(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [Authorize]
        public override async Task<GetPacienteResponse> GetPaciente(GetPacienteRequest request, ServerCallContext context)
        {
            Models.Paciente? paciente = await _context.Paciente.FirstOrDefaultAsync(p => p.id_paciente == request.IdPaciente);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            PacienteModel pacienteModel = new()
            {
                IdPaciente = paciente.id_paciente,
                Nombre = paciente.nombre,
                Cedula = paciente.cedula,
                FechaNacimiento = paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                Telefono = paciente.telefono,
                Direccion = paciente.direccion,
                CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
            };

            return new GetPacienteResponse { Paciente = pacienteModel };
        }

        // Create (Existente)
        [Authorize]
        public override async Task<CrearPacienteResponse> CrearPaciente(CrearPacienteRequest request, ServerCallContext context)
        {
            if (!DateOnly.TryParse(request.FechaNacimiento, out DateOnly fechaNacimiento))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Formato de fecha inválido"));
            }

            var nuevoPaciente = new Paciente
            {
                nombre = request.Nombre,
                cedula = request.Cedula,
                fecha_nacimiento = fechaNacimiento,
                telefono = request.Telefono,
                direccion = request.Direccion,
                id_centro_medico = request.IdCentroMedico
            };

            _context.Paciente.Add(nuevoPaciente);
            _ = await _context.SaveChangesAsync();

            PacienteModel pacienteCreado = new()
            {
                IdPaciente = nuevoPaciente.id_paciente,
                Nombre = nuevoPaciente.nombre,
                Cedula = nuevoPaciente.cedula,
                FechaNacimiento = nuevoPaciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                Telefono = nuevoPaciente.telefono,
                Direccion = nuevoPaciente.direccion,
                CentroMedico = this.GetCentro_Medico(nuevoPaciente.id_centro_medico, context).Result
            };

            return new CrearPacienteResponse { Paciente = pacienteCreado };
        }
        [Authorize]
        public override async Task<ActualizarPacienteResponse> ActualizarPaciente(ActualizarPacienteRequest request, ServerCallContext context)
        {
            var paciente = await _context.Paciente.FindAsync(request.IdPaciente);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            if (!DateOnly.TryParse(request.FechaNacimiento, out DateOnly fechaNacimiento))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Formato de fecha inválido"));
            }

            paciente.nombre = request.Nombre;
            paciente.cedula = request.Cedula;
            paciente.fecha_nacimiento = fechaNacimiento;
            paciente.telefono = request.Telefono;
            paciente.direccion = request.Direccion;
            paciente.id_centro_medico = request.IdCentroMedico;

            await _context.SaveChangesAsync();

            return new ActualizarPacienteResponse
            {
                Paciente = new PacienteModel
                {
                    IdPaciente = paciente.id_paciente,
                    Nombre = paciente.nombre,
                    Cedula = paciente.cedula,
                    FechaNacimiento = paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                    Telefono = paciente.telefono,
                    Direccion = paciente.direccion,
                    CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
                }
            };
        }
        [Authorize]
        public override async Task<EliminarPacienteResponse> EliminarPaciente(EliminarPacienteRequest request, ServerCallContext context)
        {
            var paciente = await _context.Paciente.FindAsync(request.IdPaciente);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            _context.Paciente.Remove(paciente);
            await _context.SaveChangesAsync();

            return new EliminarPacienteResponse { Success = true };
        }
        [Authorize]
        public override async Task<GetPacienteListaResponse> GetAllPaciente(Consulltorio_Medico_Consultas.Protos.EmptyResponse request, ServerCallContext context)
        {
            var pacientes = await _context.Paciente.ToListAsync();
            var pacientesLista = new List<PacienteModel>();
            foreach (var paciente in pacientes)
            {


                pacientesLista.Add(new PacienteModel
                {
                    IdPaciente = paciente.id_paciente,
                    Cedula = paciente.cedula,
                    Direccion = paciente.direccion,
                    FechaNacimiento = paciente.fecha_nacimiento.ToString(),
                    Nombre = paciente.nombre,
                    Telefono = paciente.telefono,
                    CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
                });
            }
            return new GetPacienteListaResponse
            {
                Pacientes = { pacientesLista }
            };
        }
        private async Task<Centro_Medico> GetCentro_Medico(int id_centro_medico, ServerCallContext context)
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

            var centro_Medico = await cliente.GetCentro_MedicoAsync(new Centro_MedicoGet { Id = id_centro_medico }, this.callOptionsToken(context));

            if (centro_Medico == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Centro Medico no encontrado"));
            }

            return centro_Medico;
        }
        private CallOptions callOptionsToken(ServerCallContext context)
        {
            var authHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization");
            var token = authHeader?.Value;
            var metadata = new Metadata {
                { "Authorization",token}
            };
            return new CallOptions(headers: metadata);
        }
    }
}
