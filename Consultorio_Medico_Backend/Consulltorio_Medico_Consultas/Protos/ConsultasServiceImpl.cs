using Consulltorio_Medico_Consultas.Data;
using Consulltorio_Medico_Consultas.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ConsultasMedicas;
using Consultas_Pacientes.Protos;
using Servicio_Administracion.Administracion;

namespace Consulltorio_Medico_Consultas.protos
{
    public class ConsultasServiceImpl : ConsultasService.ConsultasServiceBase
    {
        private readonly DataContext _context;

        private readonly IConfiguration _config;

        public ConsultasServiceImpl(DataContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [Authorize]
        public override async Task<Consulta> GetConsultaCedula(ConsultaCedulaRequest request, ServerCallContext context)
        {
            // Buscar el paciente por cédula
            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.cedula == request.Cedula);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            // Buscar la consulta asociada al paciente
            var consulta = await _context.ConsultasMedicas
                .FirstOrDefaultAsync(c => c.paciente.id_paciente == paciente.id_paciente);

            if (consulta == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));
            }

            // Obtener el médico (empleado) desde el microservicio de administración

            var empleadoResponse = GetEmpleado(consulta.id_empleado, context).Result;
            if (empleadoResponse == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            }

            // Mapear los datos a la respuesta gRPC
            var consultaResponse = new Consulta
            {
                // IdConsultaMedica = consulta.id_consulta_medica,
                Fecha = consulta.fecha.ToString("yyyy-MM-dd"),
                Hora = consulta.hora,
                Motivo = consulta.motivo,
                Diagnostico = consulta.diagnostico,
                Tratamiento = consulta.tratamiento,
                Paciente = new PacienteModel
                {
                    IdPaciente = paciente.id_paciente,
                    Nombre = paciente.nombre,
                    Cedula = paciente.cedula,
                    FechaNacimiento = paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                    Telefono = paciente.telefono,
                    Direccion = paciente.direccion,
                    CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
                },
                Empleado = empleadoResponse,
                CentroMedico = this.GetCentro_Medico(consulta.id_centro_medico, context).Result
            };

            return consultaResponse;
        }
        [Authorize]
        public override async Task<Consulta> CreateConsulta(CreateConsultaRequest request, ServerCallContext context)
        {
            // Validación básica
            if (string.IsNullOrEmpty(request.Fecha) || string.IsNullOrEmpty(request.Hora) ||
                string.IsNullOrEmpty(request.Motivo) || string.IsNullOrEmpty(request.Cedula))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Datos de consulta incompletos"));
            }

            // Buscar el paciente por cédula
            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.cedula == request.Cedula);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado por la cédula"));
            }


            var empleadoResponse = GetEmpleado(request.IdMedico, context).Result;

            if (empleadoResponse == null || empleadoResponse.Id == 0)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            }

            // Crear la nueva consulta usando el constructor personalizado
            var nuevaConsulta = new ConsultasMedicasEntity(
                fecha: DateOnly.Parse(request.Fecha),
                hora: request.Hora,
                motivo: request.Motivo,
                diagnostico: request.Diagnostico ?? "Sin diagnóstico",
                tratamiento: request.Tratamiento ?? "Sin tratamiento",
                id_empleado: request.IdMedico,
                paciente: paciente,
                id_centroMedico: request.IdCentroMedico
            );



            // Guardar en base de datos
            _context.ConsultasMedicas.Add(nuevaConsulta);
            var resultado = await _context.SaveChangesAsync();

            // Devolver la respuesta
            var consultaResponse = new Consulta
            {
                IdConsultaMedica = nuevaConsulta.id_consulta_medica,
                Fecha = nuevaConsulta.fecha.ToString("yyyy-MM-dd"),
                Hora = nuevaConsulta.hora,
                Motivo = nuevaConsulta.motivo,
                Diagnostico = nuevaConsulta.diagnostico,
                Tratamiento = nuevaConsulta.tratamiento,
                Paciente = new PacienteModel
                {
                    IdPaciente = paciente.id_paciente,
                    Nombre = paciente.nombre,
                    Cedula = paciente.cedula,
                    FechaNacimiento = paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                    Telefono = paciente.telefono,
                    Direccion = paciente.direccion,
                    CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
                },
                Empleado = empleadoResponse,
                CentroMedico = this.GetCentro_Medico(nuevaConsulta.id_centro_medico, context).Result
            };

            return consultaResponse;
        }
        [Authorize]
        public override async Task<Consulta> ActualizarConsulta(UpdateConsultaRequest request, ServerCallContext context)
        {
            // Validación básica
            if (request.IdConsultaMedica == 0 ||
                string.IsNullOrEmpty(request.Fecha) ||
                string.IsNullOrEmpty(request.Hora) ||
                string.IsNullOrEmpty(request.Motivo) ||
                string.IsNullOrEmpty(request.Cedula))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Datos de consulta incompletos"));
            }

            // Buscar la consulta existente
            var consultaExistente = await _context.ConsultasMedicas
                .Include(c => c.paciente)
                .FirstOrDefaultAsync(c => c.id_consulta_medica == request.IdConsultaMedica);

            if (consultaExistente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta médica no encontrada"));
            }

            // Buscar paciente por cédula
            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.cedula == request.Cedula);

            if (paciente == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado por la cédula"));
            }

            // Buscar el médico (empleado) relacionado
            var empleadoResponse = GetEmpleado(request.IdMedico, context).Result;

            if (empleadoResponse == null || empleadoResponse.Id == 0)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            }

            // Actualizar los campos de la consulta
            consultaExistente.fecha = DateOnly.Parse(request.Fecha);
            consultaExistente.hora = request.Hora;
            consultaExistente.motivo = request.Motivo;
            consultaExistente.diagnostico = request.Diagnostico ?? "Sin diagnóstico";
            consultaExistente.tratamiento = request.Tratamiento ?? "Sin tratamiento";
            consultaExistente.id_empleado = request.IdMedico;
            consultaExistente.paciente = paciente;
            consultaExistente.id_centro_medico = request.IdCentroMedico;

            // Guardar cambios
            await _context.SaveChangesAsync();

            // Devolver la respuesta
            var consultaResponse = new Consulta
            {
                IdConsultaMedica = consultaExistente.id_consulta_medica,
                Fecha = consultaExistente.fecha.ToString("yyyy-MM-dd"),
                Hora = consultaExistente.hora,
                Motivo = consultaExistente.motivo,
                Diagnostico = consultaExistente.diagnostico,
                Tratamiento = consultaExistente.tratamiento,
                Paciente = new PacienteModel
                {
                    IdPaciente = paciente.id_paciente,
                    Nombre = paciente.nombre,
                    Cedula = paciente.cedula,
                    FechaNacimiento = paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                    Telefono = paciente.telefono,
                    Direccion = paciente.direccion,
                    CentroMedico = this.GetCentro_Medico(paciente.id_centro_medico, context).Result
                },
                Empleado = empleadoResponse,
                CentroMedico = this.GetCentro_Medico(consultaExistente.id_centro_medico, context).Result
            };

            return consultaResponse;
        }

        //eliminar una consulta 
        [Authorize]
        public override async Task<ConsultasMedicas.EmptyResponse> DeleteConsulta(DeleteConsultaRequest request, ServerCallContext context)
        {
            try
            {
                // Buscar la consulta por ID
                var consulta = await _context.ConsultasMedicas
                    .FirstOrDefaultAsync(c => c.id_consulta_medica == request.IdConsultaMedica);

                if (consulta == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));
                }

                // Eliminar la consulta
                _context.ConsultasMedicas.Remove(consulta);
                var resultado = await _context.SaveChangesAsync();

                if (resultado == 0)
                {
                    throw new RpcException(new Status(StatusCode.Internal, "Error al eliminar la consulta"));
                }

                // Devolver respuesta vacía
                return new ConsultasMedicas.EmptyResponse(); // Puedes agregar un campo "mensaje" si tu EmptyResponse lo permite
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al eliminar la consulta: {ex.Message}"));
            }
        }

        //Obtener todas las consultas
        [Authorize]
        public override async Task<ConsultaList> GetAllConsultas(ConsultasMedicas.EmptyResponse request, ServerCallContext context)
        {
            // Obtener todas las consultas de la base de datos, incluyendo los pacientes relacionados
            var consultas = await _context.ConsultasMedicas
                .Include(c => c.paciente) // Incluir el paciente relacionado para evitar consultas adicionales
                .ToListAsync();

            // Si no hay consultas, devolver una lista vacía
            if (consultas == null || !consultas.Any())
            {
                return new ConsultaList();
            }

            // Crear la lista de mensajes Consulta para la respuesta
            var consultaResponses = new List<Consulta>();


            // Mapear cada consulta de la base de datos a un mensaje Consulta del proto
            foreach (var consulta in consultas)
            {
                // Obtener el médico (empleado) desde el microservicio de administración
                var empleadoResponse = GetEmpleado(consulta.id_empleado, context).Result;

                if (empleadoResponse == null || empleadoResponse.Id == 0)
                {
                    // Si no se encuentra el médico, podemos decidir cómo manejar esto
                    // Opción 1: Saltar esta consulta
                    continue;

                    // Opción 2: Lanzar una excepción (descomentar si prefieres este enfoque)
                    // throw new RpcException(new Status(StatusCode.NotFound, $"Médico no encontrado para la consulta con ID {consulta.id_consulta_medica}"));
                }

                // Mapear los datos de la consulta al mensaje Consulta del proto
                var consultaResponse = new Consulta
                {
                    IdConsultaMedica = consulta.id_consulta_medica,
                    Fecha = consulta.fecha.ToString("yyyy-MM-dd"),
                    Hora = consulta.hora,
                    Motivo = consulta.motivo,
                    Diagnostico = consulta.diagnostico,
                    Tratamiento = consulta.tratamiento,
                    Paciente = new PacienteModel
                    {
                        IdPaciente = consulta.paciente.id_paciente,
                        Nombre = consulta.paciente.nombre,
                        Cedula = consulta.paciente.cedula,
                        FechaNacimiento = consulta.paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                        Telefono = consulta.paciente.telefono,
                        Direccion = consulta.paciente.direccion,
                        CentroMedico = this.GetCentro_Medico(consulta.paciente.id_centro_medico, context).Result
                    },
                    Empleado = empleadoResponse,
                    CentroMedico = this.GetCentro_Medico(consulta.id_centro_medico, context).Result
                };

                consultaResponses.Add(consultaResponse);
            }

            // Devolver la lista de consultas en un mensaje ConsultaList
            return new ConsultaList { Consultas = { consultaResponses } };
        }
        [Authorize]
        public override async Task<ConsultaList> GetConsultasReporte(ConsultaCedulaRequest request, ServerCallContext context)
        {
            // Obtener todas las consultas de la base de datos, incluyendo los pacientes relacionados
            var consultas = await _context.ConsultasMedicas
                .Include(c => c.paciente) // Incluir el paciente relacionado para evitar consultas adicionales
                .Where(c => c.paciente.cedula == request.Cedula)
                .ToListAsync();

            // Si no hay consultas, devolver una lista vacía
            if (consultas == null || !consultas.Any())
            {
                return new ConsultaList();
            }

            // Crear la lista de mensajes Consulta para la respuesta
            var consultaResponses = new List<Consulta>();



            // Mapear cada consulta de la base de datos a un mensaje Consulta del proto
            foreach (var consulta in consultas)
            {
                // Obtener el médico (empleado) desde el microservicio de administración
                var empleadoResponse = GetEmpleado(consulta.id_empleado, context).Result;

                if (empleadoResponse == null || empleadoResponse.Id == 0)
                {
                    // Si no se encuentra el médico, podemos decidir cómo manejar esto
                    // Opción 1: Saltar esta consulta
                    continue;

                    // Opción 2: Lanzar una excepción (descomentar si prefieres este enfoque)
                    // throw new RpcException(new Status(StatusCode.NotFound, $"Médico no encontrado para la consulta con ID {consulta.id_consulta_medica}"));
                }

                // Mapear los datos de la consulta al mensaje Consulta del proto
                var consultaResponse = new Consulta
                {
                    IdConsultaMedica = consulta.id_consulta_medica,
                    Fecha = consulta.fecha.ToString("yyyy-MM-dd"),
                    Hora = consulta.hora,
                    Motivo = consulta.motivo,
                    Diagnostico = consulta.diagnostico,
                    Tratamiento = consulta.tratamiento,
                    Paciente = new PacienteModel
                    {
                        IdPaciente = consulta.paciente.id_paciente,
                        Nombre = consulta.paciente.nombre,
                        Cedula = consulta.paciente.cedula,
                        FechaNacimiento = consulta.paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                        Telefono = consulta.paciente.telefono,
                        Direccion = consulta.paciente.direccion,
                        CentroMedico = this.GetCentro_Medico(consulta.paciente.id_centro_medico, context).Result
                    },
                    Empleado = empleadoResponse,
                    CentroMedico = this.GetCentro_Medico(consulta.id_centro_medico, context).Result
                };

                consultaResponses.Add(consultaResponse);
            }

            // Devolver la lista de consultas en un mensaje ConsultaList
            return new ConsultaList { Consultas = { consultaResponses } };
        }
        [Authorize]
        public override async Task<ConsultaList> GetConsultasCentroMedico(Centro_MedicoGet request, ServerCallContext context)
        {
            // Obtener todas las consultas de la base de datos, incluyendo los pacientes relacionados
            var consultas = await _context.ConsultasMedicas
                .Include(c => c.paciente) // Incluir el paciente relacionado para evitar consultas adicionales
                .Where(c => c.id_centro_medico == request.Id)
                .ToListAsync();

            // Si no hay consultas, devolver una lista vacía
            if (consultas == null || !consultas.Any())
            {
                return new ConsultaList();
            }

            // Crear la lista de mensajes Consulta para la respuesta
            var consultaResponses = new List<Consulta>();

            // Configurar el canal gRPC para el microservicio de administración

            // Mapear cada consulta de la base de datos a un mensaje Consulta del proto
            foreach (var consulta in consultas)
            {
                // Obtener el médico (empleado) desde el microservicio de administración
                var empleadoResponse = GetEmpleado(consulta.id_empleado, context).Result;

                if (empleadoResponse == null || empleadoResponse.Id == 0)
                {
                    // Si no se encuentra el médico, podemos decidir cómo manejar esto
                    // Opción 1: Saltar esta consulta
                    continue;

                    // Opción 2: Lanzar una excepción (descomentar si prefieres este enfoque)
                    // throw new RpcException(new Status(StatusCode.NotFound, $"Médico no encontrado para la consulta con ID {consulta.id_consulta_medica}"));
                }

                // Mapear los datos de la consulta al mensaje Consulta del proto
                var consultaResponse = new Consulta
                {
                    IdConsultaMedica = consulta.id_consulta_medica,
                    Fecha = consulta.fecha.ToString("yyyy-MM-dd"),
                    Hora = consulta.hora,
                    Motivo = consulta.motivo,
                    Diagnostico = consulta.diagnostico,
                    Tratamiento = consulta.tratamiento,
                    Paciente = new PacienteModel
                    {
                        IdPaciente = consulta.paciente.id_paciente,
                        Nombre = consulta.paciente.nombre,
                        Cedula = consulta.paciente.cedula,
                        FechaNacimiento = consulta.paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                        Telefono = consulta.paciente.telefono,
                        Direccion = consulta.paciente.direccion,
                        CentroMedico = this.GetCentro_Medico(consulta.paciente.id_centro_medico, context).Result
                    },
                    Empleado = empleadoResponse,
                    CentroMedico = this.GetCentro_Medico(consulta.id_centro_medico, context).Result
                };
                consultaResponses.Add(consultaResponse);

            }

            // Devolver la lista de consultas en un mensaje ConsultaList
            return new ConsultaList { Consultas = { consultaResponses } };
        }
        [Authorize]
        public override async Task<ConsultaList> GetConsultasByFecha(ConsultaFechaRequest request, ServerCallContext context)
        {
            // Obtener todas las consultas de la base de datos, incluyendo los pacientes relacionados
            var consultas = await _context.ConsultasMedicas
                .Include(c => c.paciente) // Incluir el paciente relacionado para evitar consultas adicionales
                .Where(c => c.fecha >= DateOnly.ParseExact(request.FechaDesde, "yyyy/MM/dd") && c.fecha <= DateOnly.ParseExact(request.FechaHasta, "yyyy/MM/dd"))
                .ToListAsync();

            // Si no hay consultas, devolver una lista vacía
            if (consultas == null || !consultas.Any())
            {
                return new ConsultaList();
            }

            // Crear la lista de mensajes Consulta para la respuesta
            var consultaResponses = new List<Consulta>();


            // Mapear cada consulta de la base de datos a un mensaje Consulta del proto
            foreach (var consulta in consultas)
            {
                // Obtener el médico (empleado) desde el microservicio de administración
                var empleadoRequest = new EmpleadoGet { Id = consulta.id_empleado };
                var empleadoResponse = GetEmpleado(consulta.id_empleado, context).Result;

                if (empleadoResponse == null || empleadoResponse.Id == 0)
                {
                    // Si no se encuentra el médico, podemos decidir cómo manejar esto
                    // Opción 1: Saltar esta consulta
                    continue;

                    // Opción 2: Lanzar una excepción (descomentar si prefieres este enfoque)
                    // throw new RpcException(new Status(StatusCode.NotFound, $"Médico no encontrado para la consulta con ID {consulta.id_consulta_medica}"));
                }

                // Mapear los datos de la consulta al mensaje Consulta del proto
                var consultaResponse = new Consulta
                {
                    IdConsultaMedica = consulta.id_consulta_medica,
                    Fecha = consulta.fecha.ToString("yyyy-MM-dd"),
                    Hora = consulta.hora,
                    Motivo = consulta.motivo,
                    Diagnostico = consulta.diagnostico,
                    Tratamiento = consulta.tratamiento,
                    Paciente = new PacienteModel
                    {
                        IdPaciente = consulta.paciente.id_paciente,
                        Nombre = consulta.paciente.nombre,
                        Cedula = consulta.paciente.cedula,
                        FechaNacimiento = consulta.paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                        Telefono = consulta.paciente.telefono,
                        Direccion = consulta.paciente.direccion,
                        CentroMedico = this.GetCentro_Medico(consulta.paciente.id_centro_medico, context).Result
                    },
                    Empleado = empleadoResponse,
                    CentroMedico = this.GetCentro_Medico(consulta.id_centro_medico, context).Result
                };

                consultaResponses.Add(consultaResponse);
            }

            // Devolver la lista de consultas en un mensaje ConsultaList
            return new ConsultaList { Consultas = { consultaResponses } };
        }
        private async Task<Empleado> GetEmpleado(int idEmpleado, ServerCallContext context)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var canal = GrpcChannel.ForAddress(_config["grcp:administracion"], new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            var cliente = new AdministracionService.AdministracionServiceClient(canal);
            var empleado = await cliente.GetEmpleadoAsync(new EmpleadoGet { Id = idEmpleado }, callOptionsToken(context));

            if (empleado == null || empleado.Id == 0)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            }

            return empleado;
        }

        private CallOptions callOptionsToken(ServerCallContext context)
        {
            var authHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization");
            var token = authHeader?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token no proporcionado"));
            }

            var metadata = new Metadata
    {
        { "Authorization", token }
    };

            return new CallOptions(headers: metadata);
        }

        private async Task<Centro_Medico> GetCentro_Medico(int id_centro_medico, ServerCallContext context)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var canal = GrpcChannel.ForAddress(_config["grcp:administracion"], new GrpcChannelOptions
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

    }
}
