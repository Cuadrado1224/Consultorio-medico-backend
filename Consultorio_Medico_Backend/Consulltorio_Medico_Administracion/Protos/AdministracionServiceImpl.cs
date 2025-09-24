
using Microsoft.EntityFrameworkCore;
using Grpc.Core;
using Consulltorio_Medico_Administracion.Data;

using Microsoft.AspNetCore.Authorization;
using Consulltorio_Medico_Administracion.Administracion;

namespace Consultorio_Medico_Administracion.Protos
{
    public class AdministracionServiceImpl : AdministracionService.AdministracionServiceBase
    {
        private readonly AppDbContext _context;

        public AdministracionServiceImpl(AppDbContext context)
        {
            _context = context;
        }
        [Authorize]
        public override async Task<EmpleadoLista> GetAllEmpleado(Consulltorio_Medico_Administracion.Administracion.RespuestaVacia request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado id no puede ser null"));
            }
            var e = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .ToListAsync();

            var empleadosLista =new List<Consulltorio_Medico_Administracion.Administracion.Empleado>();

            foreach (var u in e)
            {
                var empleadoFila = new Consulltorio_Medico_Administracion.Administracion.Empleado
                {
                    Id = u.Id,
                    Cedula = u.cedula,
                    CentroMedicoID = u.centro_medicoID,
                    Email = u.email,
                    EspecialidadID = u.especialidadID,
                    Nombre = u.nombre,
                    Salario = u.salario,
                    Telefono = u.telefono,
                    TipoEmpleadoID = u.especialidadID,
                    CentroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                    {
                        Id = u.Centro_Medico.Id,
                        Ciudad = u.Centro_Medico.ciudad,
                        Direccion = u.Centro_Medico.direccion,
                        Nombre = u.Centro_Medico.nombre
                    },
                    Especialidad = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                    {
                        Id = u.Especialidad.Id,
                        Especialidad_ = u.Especialidad.especialidad
                    },
                    TipoEmpleado = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                    {
                        Id = u.Tipo_Empleado.Id,
                        Tipo = u.Tipo_Empleado.tipo
                    }
                };
                empleadosLista.Add(empleadoFila);
            }

            return new EmpleadoLista
            {
                Empleados = { empleadosLista }
            };
        }
        [Authorize]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Empleado> GetEmpleado(EmpleadoGet request, ServerCallContext context)
        {

            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El Empleado no puede ser null"));
            }

            var e = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .FirstOrDefaultAsync(e => e.Id == request.Id);
            if (e == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Empleado no encontrado"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Empleado
            {
                Email=e.email,
                Id=e.Id,
                Cedula=e.cedula,
                Nombre=e.nombre,
                CentroMedicoID=e.centro_medicoID,
                EspecialidadID=e.especialidadID,
                TipoEmpleadoID=e.tipo_empleadoID,
                Salario=e.salario,
                Telefono=e.telefono,
                Especialidad=new Consulltorio_Medico_Administracion.Administracion.Especialidad
                {
                    Especialidad_=e.Especialidad.especialidad,
                    Id=e.Especialidad.Id

                },
                TipoEmpleado=new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                {
                    Id=e.Tipo_Empleado.Id,
                    Tipo=e.Tipo_Empleado.tipo
                },
                CentroMedico=new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                {
                    Id=e.Centro_Medico.Id,
                    Ciudad=e.Centro_Medico.ciudad,
                    Direccion=e.Centro_Medico.direccion,
                    Nombre=e.Centro_Medico.nombre
                }

            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.RespuestaVacia> DeleteEmpleado(EmpleadoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado id no puede ser null"));
            }
            var empleado = await _context.Empleados.FindAsync(request.Id);
            if (empleado == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El empleado no existe"));
            }

            _context.Empleados.Remove(empleado);
            await _context.SaveChangesAsync();

            return new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Empleado> PostEmpleado(EmpleadoPost request, ServerCallContext context)
        {

            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado no puede ser null"));
            }

            // Verificaciones de claves foráneas
            var centroExiste = await _context.Centros_Medicos.AnyAsync(cm => cm.Id == request.CentroMedicoID);
            if (!centroExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Centro médico no válido."));

            var especialidadExiste = await _context.Especialidades.AnyAsync(e => e.Id == request.EspecialidadID);
            if (!especialidadExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Especialidad no válida."));

            var tipoEmpleadoExiste = await _context.Tipos_Empleados.AnyAsync(t => t.Id == request.TipoEmpleadoID);
            if (!tipoEmpleadoExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Tipo de empleado no válido."));

            var empleadoGuardar = new Consulltorio_Medico_Administracion.Models.Empleado
            {
                cedula = request.Cedula,
                email = request.Email,
                nombre = request.Nombre,
                telefono = request.Telefono,
                salario = request.Salario,
                centro_medicoID = request.CentroMedicoID,
                especialidadID = request.EspecialidadID,
                tipo_empleadoID = request.TipoEmpleadoID,
            };

            try
            {
                _context.Empleados.Add(empleadoGuardar);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el empleado: {ex.InnerException?.Message}"));
            }

            var empleadoConDatos = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .FirstOrDefaultAsync(e => e.Id == empleadoGuardar.Id);

            if (empleadoConDatos == null)
                throw new RpcException(new Status(StatusCode.Internal, "Error al recuperar el empleado recién creado."));

            return new Consulltorio_Medico_Administracion.Administracion.Empleado
            {
                Id = empleadoConDatos.Id,
                Cedula = empleadoConDatos.cedula,
                CentroMedicoID = empleadoConDatos.centro_medicoID,
                Email = empleadoConDatos.email,
                EspecialidadID = empleadoConDatos.especialidadID,
                Nombre = empleadoConDatos.nombre,
                Salario = empleadoConDatos.salario,
                Telefono = empleadoConDatos.telefono,
                TipoEmpleadoID = empleadoConDatos.tipo_empleadoID,
                CentroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                {
                    Id = empleadoConDatos.Centro_Medico.Id,
                    Ciudad = empleadoConDatos.Centro_Medico.ciudad,
                    Direccion = empleadoConDatos.Centro_Medico.direccion,
                    Nombre = empleadoConDatos.Centro_Medico.nombre
                },
                Especialidad = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                {
                    Id = empleadoConDatos.Especialidad.Id,
                    Especialidad_ = empleadoConDatos.Especialidad.especialidad
                },
                TipoEmpleado = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                {
                    Id = empleadoConDatos.Tipo_Empleado.Id,
                    Tipo = empleadoConDatos.Tipo_Empleado.tipo
                }
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Empleado> PutEmpleado(EmpleadoPut request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado no puede ser null"));
            }

            // Verificaciones de claves foráneas
            var empleadoExiste = await _context.Empleados.AnyAsync(cm => cm.Id == request.Id);
            if (!empleadoExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Empleado no válido."));

            var centroExiste = await _context.Centros_Medicos.AnyAsync(cm => cm.Id == request.CentroMedicoID);
            if (!centroExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Centro médico no válido."));

            var especialidadExiste = await _context.Especialidades.AnyAsync(e => e.Id == request.EspecialidadID);
            if (!especialidadExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Especialidad no válida."));

            var tipoEmpleadoExiste = await _context.Tipos_Empleados.AnyAsync(t => t.Id == request.TipoEmpleadoID);
            if (!tipoEmpleadoExiste)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Tipo de empleado no válido."));

            var empleadoGuardar = new Consulltorio_Medico_Administracion.Models.Empleado
            {
                Id=request.Id,
                cedula = request.Cedula,
                email = request.Email,
                nombre = request.Nombre,
                telefono = request.Telefono,
                salario = request.Salario,
                centro_medicoID = request.CentroMedicoID,
                especialidadID = request.EspecialidadID,
                tipo_empleadoID = request.TipoEmpleadoID,
            };

            try
            {
                _context.Entry(empleadoGuardar).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el empleado: {ex.InnerException?.Message}"));
            }

            var empleadoConDatos = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .FirstOrDefaultAsync(e => e.Id == empleadoGuardar.Id);

            if (empleadoConDatos == null)
                throw new RpcException(new Status(StatusCode.Internal, "Error al recuperar el empleado recién creado."));

            return new Consulltorio_Medico_Administracion.Administracion.Empleado
            {
                Id = empleadoConDatos.Id,
                Cedula = empleadoConDatos.cedula,
                CentroMedicoID = empleadoConDatos.centro_medicoID,
                Email = empleadoConDatos.email,
                EspecialidadID = empleadoConDatos.especialidadID,
                Nombre = empleadoConDatos.nombre,
                Salario = empleadoConDatos.salario,
                Telefono = empleadoConDatos.telefono,
                TipoEmpleadoID = empleadoConDatos.tipo_empleadoID,
                CentroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                {
                    Id = empleadoConDatos.Centro_Medico.Id,
                    Ciudad = empleadoConDatos.Centro_Medico.ciudad,
                    Direccion = empleadoConDatos.Centro_Medico.direccion,
                    Nombre = empleadoConDatos.Centro_Medico.nombre
                },
                Especialidad = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                {
                    Id = empleadoConDatos.Especialidad.Id,
                    Especialidad_ = empleadoConDatos.Especialidad.especialidad
                },
                TipoEmpleado = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                {
                    Id = empleadoConDatos.Tipo_Empleado.Id,
                    Tipo = empleadoConDatos.Tipo_Empleado.tipo
                }
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<EmpleadoLista> GetAllEmpleadoByCentroMedico(Centro_MedicoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado id no puede ser null"));
            }
            var e = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .Where(e=>e.centro_medicoID.Equals(request.Id))
                .ToListAsync();

            var empleadosLista = new List<Consulltorio_Medico_Administracion.Administracion.Empleado>();

            foreach (var u in e)
            {
                var empleadoFila = new Consulltorio_Medico_Administracion.Administracion.Empleado
                {
                    Id = u.Id,
                    Cedula = u.cedula,
                    CentroMedicoID = u.centro_medicoID,
                    Email = u.email,
                    EspecialidadID = u.especialidadID,
                    Nombre = u.nombre,
                    Salario = u.salario,
                    Telefono = u.telefono,
                    TipoEmpleadoID = u.especialidadID,
                    CentroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                    {
                        Id = u.Centro_Medico.Id,
                        Ciudad = u.Centro_Medico.ciudad,
                        Direccion = u.Centro_Medico.direccion,
                        Nombre = u.Centro_Medico.nombre
                    },
                    Especialidad = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                    {
                        Id = u.Especialidad.Id,
                        Especialidad_ = u.Especialidad.especialidad
                    },
                    TipoEmpleado = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                    {
                        Id = u.Tipo_Empleado.Id,
                        Tipo = u.Tipo_Empleado.tipo
                    }
                };
                empleadosLista.Add(empleadoFila);
            }

            return new EmpleadoLista
            {
                Empleados = { empleadosLista }
            };
        }
        public override async Task<EmpleadoLista> GetAllEmpleadoByEspecialidad(EspecialidadGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El empleado id no puede ser null"));
            }
            var e = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Especialidad)
                .Include(e => e.Tipo_Empleado)
                .Where(e => e.especialidadID.Equals(request.Id))
                .ToListAsync();

            var empleadosLista = new List<Consulltorio_Medico_Administracion.Administracion.Empleado>();

            foreach (var u in e)
            {
                var empleadoFila = new Consulltorio_Medico_Administracion.Administracion.Empleado
                {
                    Id = u.Id,
                    Cedula = u.cedula,
                    CentroMedicoID = u.centro_medicoID,
                    Email = u.email,
                    EspecialidadID = u.especialidadID,
                    Nombre = u.nombre,
                    Salario = u.salario,
                    Telefono = u.telefono,
                    TipoEmpleadoID = u.especialidadID,
                    CentroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                    {
                        Id = u.Centro_Medico.Id,
                        Ciudad = u.Centro_Medico.ciudad,
                        Direccion = u.Centro_Medico.direccion,
                        Nombre = u.Centro_Medico.nombre
                    },
                    Especialidad = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                    {
                        Id = u.Especialidad.Id,
                        Especialidad_ = u.Especialidad.especialidad
                    },
                    TipoEmpleado = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                    {
                        Id = u.Tipo_Empleado.Id,
                        Tipo = u.Tipo_Empleado.tipo
                    }
                };
                empleadosLista.Add(empleadoFila);
            }

            return new EmpleadoLista
            {
                Empleados = { empleadosLista }
            };
        }
        //////////////
        [Authorize]
        public override async Task<Centro_MedicoLista> GetAllCentro_Medico(Consulltorio_Medico_Administracion.Administracion.RespuestaVacia request, ServerCallContext context)
        {
            var centrosMedicos = await _context.Centros_Medicos.ToListAsync();
            
            var CentrosLista=new List<Consulltorio_Medico_Administracion.Administracion.Centro_Medico>();

            foreach (var centro in centrosMedicos)
            {
                var centroMedico = new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
                {
                    Id = centro.Id,
                    Ciudad = centro.ciudad,
                    Direccion = centro.direccion,
                    Nombre = centro.nombre
                };
                CentrosLista.Add(centroMedico);
            }


            return new Centro_MedicoLista {
                Centros = { CentrosLista}
            };
        }
        [Authorize]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Centro_Medico> GetCentro_Medico(Centro_MedicoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El Centro Medico no puede ser null"));
            }
            var centrosMedico = await _context.Centros_Medicos.FirstOrDefaultAsync(c=> c.Id==request.Id);

            if (centrosMedico == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El Centro Medico no Existe"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
            {
             Id=centrosMedico.Id,
             Ciudad=centrosMedico.ciudad,
             Direccion=centrosMedico.direccion,
             Nombre=centrosMedico.nombre
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Centro_Medico> PostCentro_Medico(Centro_MedicoPost request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El centro medico no puede ser null"));
            }


            var CentroGuardar = new Consulltorio_Medico_Administracion.Models.Centro_Medico
            {
                ciudad=request.Ciudad,
                direccion=request.Direccion,
                nombre=request.Nombre,
                
            };

            try
            {
                _context.Centros_Medicos.Add(CentroGuardar);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el centro medico: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
            {
                Id = CentroGuardar.Id,
                Ciudad = CentroGuardar.ciudad,
                Direccion = CentroGuardar.direccion,
                Nombre = CentroGuardar.nombre
            };
            
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Centro_Medico> PutCentro_Medico(Consulltorio_Medico_Administracion.Administracion.Centro_Medico request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El centro medico no puede ser null"));
            }
            var centroMedicoBuscar = await _context.Centros_Medicos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id);
            if (centroMedicoBuscar==null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El centro medico no existe"));
            }

            var CentroGuardar = new Consulltorio_Medico_Administracion.Models.Centro_Medico
            {
                ciudad = request.Ciudad,
                direccion = request.Direccion,
                nombre = request.Nombre,
                Id=request.Id
            };

            try
            {
                _context.Attach(CentroGuardar).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el centro medico: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Centro_Medico
            {
                Id = CentroGuardar.Id,
                Ciudad = CentroGuardar.ciudad,
                Direccion = CentroGuardar.direccion,
                Nombre = CentroGuardar.nombre
            };

        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.RespuestaVacia> DeleteCentro_Medico(Centro_MedicoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El centro medico id no puede ser null"));
            }
            var centro = await _context.Centros_Medicos.FindAsync(request.Id);
            if (centro == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El centro medico no existe"));
            }

            _context.Centros_Medicos.Remove(centro);
            await _context.SaveChangesAsync();

            return new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { };
        }
        //////////////////
        [Authorize]
        public override async Task<Tipo_EmpleadoLista> GetAllTipo_Empleado(Consulltorio_Medico_Administracion.Administracion.RespuestaVacia request, ServerCallContext context)
        {
            var tipoBuscar = await _context.Tipos_Empleados.ToListAsync();

            var TiposLista = new List<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado>();

            foreach (var tipo in tipoBuscar)
            {
                var tipoFila = new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
                {
                    Id = tipo.Id,
                    Tipo= tipo.tipo
                };
                TiposLista.Add(tipoFila);
            }


            return new Tipo_EmpleadoLista
            {
                Tipos = { TiposLista }
            };
        }
        [Authorize]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado> GetTipo_Empleado(Tipo_EmpleadoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El Tipo Empleado no puede ser null"));
            }
            var tipoBuscar = await _context.Tipos_Empleados.FirstOrDefaultAsync(c => c.Id == request.Id);

            if (tipoBuscar == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El Tipo Empleado no Existe"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
            {
                Id= tipoBuscar.Id,
                Tipo=tipoBuscar.tipo
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado> PostTipo_Empleado(Tipo_EmpleadoPost request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El tipo empleado no puede ser null"));
            }


            var TipoGuardar = new Consulltorio_Medico_Administracion.Models.Tipo_Empleado
            {
                tipo=request.Tipo
            };

            try
            {
                _context.Tipos_Empleados.Add(TipoGuardar);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el tipo de empleado: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
            {
                Id=TipoGuardar.Id,
                Tipo = TipoGuardar.tipo
            };

        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado> PutTipo_Empleado(Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El tipo de empleado no puede ser null"));
            }
            var tipoEmpleadoBuscar = await _context.Tipos_Empleados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id);
            if (tipoEmpleadoBuscar == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El tipo de empleado no existe"));
            }

            var tipoGuardar = new Consulltorio_Medico_Administracion.Models.Tipo_Empleado
            {
                Id=request.Id,
                tipo=request.Tipo

            };

            try
            {
                _context.Entry(tipoGuardar).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar el centro medico: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Tipo_Empleado
            {
                Id = tipoGuardar.Id,
                Tipo = tipoGuardar.tipo
            };

        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.RespuestaVacia> DeleteTipo_Empleado(Tipo_EmpleadoGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El Tipo Empleado id no puede ser null"));
            }
            var tipo = await _context.Tipos_Empleados.FindAsync(request.Id);
            if (tipo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El Tipo Empleado no existe"));
            }

            _context.Tipos_Empleados.Remove(tipo);
            await _context.SaveChangesAsync();

            return new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { };
        }

        //////////////////
        [Authorize]
        public override async Task<EspecialidadLista> GetAllEspecialidades(Consulltorio_Medico_Administracion.Administracion.RespuestaVacia request, ServerCallContext context)
        {
            var especialidadBuscar = await _context.Especialidades.ToListAsync();

            var especialidadLista = new List<Consulltorio_Medico_Administracion.Administracion.Especialidad>();

            foreach (var especialidad in especialidadBuscar)
            {
                var tipoFila = new Consulltorio_Medico_Administracion.Administracion.Especialidad
                {
                    Id = especialidad.Id,
                    Especialidad_=especialidad.especialidad
                };
                especialidadLista.Add(tipoFila);
            }


            return new EspecialidadLista
            {
                Especialidades = { especialidadLista}
            };
        }
        [Authorize]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Especialidad> GetEspecialidades(EspecialidadGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "La especialidad no puede ser null"));
            }
            var especialidadBuscar = await _context.Especialidades.FirstOrDefaultAsync(c => c.Id == request.Id);

            if (especialidadBuscar == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "La especialidad no Existe"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Especialidad
            {
                Id = especialidadBuscar.Id,
                Especialidad_=especialidadBuscar.especialidad
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Especialidad> PostEspecialidad(EspecialidadPost request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "La especialidad no puede ser null"));
            }


            var especialidadGuardar = new Consulltorio_Medico_Administracion.Models.Especialidad
            {
                especialidad=request.Especialidad
            };

            try
            {
                _context.Especialidades.Add(especialidadGuardar);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar la especialidad: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Especialidad
            {
                Id = especialidadGuardar.Id,
                Especialidad_=especialidadGuardar.especialidad
            };

        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.Especialidad> PutEspecialidad(Consulltorio_Medico_Administracion.Administracion.Especialidad request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "La especialidad no puede ser null"));
            }
            var especialidadBuscar = await _context.Especialidades.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id);
            if (especialidadBuscar == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "la Especialidad no existe"));
            }

            var especialidadGuardar = new Consulltorio_Medico_Administracion.Models.Especialidad
            {
                Id = request.Id,
                especialidad=request.Especialidad_

            };

            try
            {
                _context.Attach(especialidadGuardar).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al guardar la especialidad: {ex.InnerException?.Message}"));
            }


            return new Consulltorio_Medico_Administracion.Administracion.Especialidad
            {
                Id = especialidadGuardar.Id,
                Especialidad_=especialidadGuardar.especialidad
            };

        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Consulltorio_Medico_Administracion.Administracion.RespuestaVacia> DeleteEspecialidad(EspecialidadGet request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "La especialidad id no puede ser null"));
            }
            var especialidad = await _context.Especialidades.FindAsync(request.Id);
            if (especialidad == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "La especialidad no existe"));
            }

            _context.Especialidades.Remove(especialidad);
            await _context.SaveChangesAsync();

            return new Consulltorio_Medico_Administracion.Administracion.RespuestaVacia { };
        }


    }
}
