
using Microsoft.EntityFrameworkCore;
using Grpc.Core;
using Consulltorio_Medico_Administracion.Data;
using Consulltorio_Medico_Administracion.Protos;
using Consulltorio_Medico_Administracion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace Consulltorio_Medico_Administracion.Protos
{
    public class UsuarioServiceImpl : UsuarioService.UsuarioServiceBase
    {
        private readonly AppDbContext _context;

        public UsuarioServiceImpl(AppDbContext context)
        {
            _context = context;
        }

        public override async Task<Consulltorio_Medico_Administracion.Protos.UsuarioLoginRespuesta> ValidarUsuario(Consulltorio_Medico_Administracion.Protos.UsuarioLogin usuario, ServerCallContext context)
        {
            if (usuario == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El usuario no puede ser null"));
            }

            var usuarioBuscar = await _context.Usuarios
                .Include(u => u.empleado)
                .Include(u => u.empleado.Centro_Medico)
                .Include(u => u.empleado.Tipo_Empleado)
                .Include(u => u.empleado.Especialidad)
                .FirstOrDefaultAsync(u => u.nombre_usuario == usuario.NombreUsuario && u.contraseña == usuario.Contrasenia);

            if (usuarioBuscar == null)
            {
                return new Consulltorio_Medico_Administracion.Protos.UsuarioLoginRespuesta
                {
                    EsValido = false
                };
            }

            var UsuarioDTO= new Protos.Usuario
            {
                Contrasenia = usuarioBuscar.contraseña,
                NombreUsuario = usuarioBuscar.nombre_usuario,
                EmpleadoId = usuarioBuscar.empleadoId,
                Id = usuarioBuscar.Id,
                Empleado = new Empleado
                {
                    Id = usuarioBuscar.empleado.Id,
                    Cedula = usuarioBuscar.empleado.cedula,
                    CentroMedicoID = usuarioBuscar.empleado.centro_medicoID,
                    Email = usuarioBuscar.empleado.email,
                    EspecialidadID = usuarioBuscar.empleado.especialidadID,
                    Nombre = usuarioBuscar.empleado.nombre,
                    Salario = usuarioBuscar.empleado.salario,
                    Telefono = usuarioBuscar.empleado.telefono,
                    TipoEmpleadoID = usuarioBuscar.empleado.especialidadID,
                    CentroMedico = new Centro_Medico
                    {
                        Id = usuarioBuscar.empleado.Centro_Medico.Id,
                        Ciudad = usuarioBuscar.empleado.Centro_Medico.ciudad,
                        Direccion = usuarioBuscar.empleado.Centro_Medico.direccion,
                        Nombre = usuarioBuscar.empleado.Centro_Medico.nombre
                    },
                    Especialidad = new Especialidad
                    {
                        Id = usuarioBuscar.empleado.Especialidad.Id,
                        Especialidad_ = usuarioBuscar.empleado.Especialidad.especialidad
                    },
                    TipoEmpleado = new Tipo_Empleado
                    {
                        Id = usuarioBuscar.empleado.Tipo_Empleado.Id,
                        Tipo = usuarioBuscar.empleado.Tipo_Empleado.tipo
                    }
                }
            };

            return new UsuarioLoginRespuesta
            {
                EsValido = true,
                Usuario = UsuarioDTO
            };
        }

        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<Usuario> RegistrarUsuario(UsuarioRegistro request, ServerCallContext context)
        {
            var usuarioBuscar = await _context.Usuarios.FirstOrDefaultAsync(u => u.nombre_usuario == request.NombreUsuario);
            if (usuarioBuscar != null)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "El usuario ya existe"));
            }
            var empleado = await _context.Empleados
                .Include(u => u.Centro_Medico)
                .Include(u => u.Tipo_Empleado)
                .Include(u => u.Especialidad)
                .FirstOrDefaultAsync(e=>e.Id==request.EmpleadoId);

            if (empleado == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El empleado no existe"));
            }

            var empleadoUsuarioBuscar = await _context.Usuarios.FirstOrDefaultAsync(u => u.empleadoId == request.EmpleadoId);
            if (empleadoUsuarioBuscar != null)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "El empleado ya esta registrado"));
            }

            var usuarioGuardar=new Models.Usuario { 
                Id = 0,
                nombre_usuario = request.NombreUsuario,
                contraseña=request.Contrasenia,
                empleadoId = request.EmpleadoId,
                empleado = empleado
                
            };

            _context.Usuarios.Add(usuarioGuardar);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Error al guardar el usuario.")); 
            }



            return new Usuario
            {
                Id = usuarioGuardar.Id,
                NombreUsuario = request.NombreUsuario,
                Contrasenia = request.Contrasenia,
                EmpleadoId = request.EmpleadoId,
                Empleado = new Empleado
                {
                    Id = empleado.Id,
                    Cedula = empleado.cedula,
                    CentroMedicoID = empleado.centro_medicoID,
                    Email = empleado.email,
                    EspecialidadID = empleado.especialidadID,
                    Nombre = empleado.nombre,
                    Salario = empleado.salario,
                    Telefono = empleado.telefono,
                    TipoEmpleadoID = empleado.especialidadID,
                    CentroMedico = new Centro_Medico
                    {
                        Id = empleado.Centro_Medico.Id,
                        Ciudad = empleado.Centro_Medico.ciudad,
                        Direccion = empleado.Centro_Medico.direccion,
                        Nombre = empleado.Centro_Medico.nombre
                    },
                    Especialidad = new Especialidad
                    {
                        Id = empleado.Especialidad.Id,
                        Especialidad_ = empleado.Especialidad.especialidad
                    },
                    TipoEmpleado = new Tipo_Empleado
                    {
                        Id = empleado.Tipo_Empleado.Id,
                        Tipo = empleado.Tipo_Empleado.tipo
                    }
                }
            };
        }
        [Authorize(Policy = "TipoEmpleadoPolitica")]
        public override async Task<RespuestaVacia> BorrarUsuario(UsuarioBorrar request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El usuario id no puede ser null"));
            }
            var usuario = await _context.Usuarios.FindAsync(request.Id);
            if (usuario == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El usuario no existe"));
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return new RespuestaVacia();
        }
        [Authorize]
        public override async Task<ListaUsuarios> SeleccionarUsuarios(RespuestaVacia request, ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "El usuario id no puede ser null"));
            }
            var usuarios = await _context.Usuarios
            .Include(u => u.empleado)
            .Include(u => u.empleado.Centro_Medico)
            .Include(u => u.empleado.Tipo_Empleado)
            .Include(u => u.empleado.Especialidad)
            .ToListAsync();
            
            var usuarioLista = new List<Usuario>(); 

            foreach ( var u in usuarios)
            {

                var usuarioFila = new Protos.Usuario
                {
                    Id = u.Id,
                    NombreUsuario = u.nombre_usuario,
                    Contrasenia = u.contraseña,
                    Empleado = new Protos.Empleado
                    {
                        Id = u.empleado.Id,
                        Cedula = u.empleado.cedula,
                        CentroMedicoID = u.empleado.centro_medicoID,
                        Email = u.empleado.email,
                        EspecialidadID = u.empleado.especialidadID,
                        Nombre = u.empleado.nombre,
                        Salario = u.empleado.salario,
                        Telefono = u.empleado.telefono,
                        TipoEmpleadoID = u.empleado.especialidadID,
                        CentroMedico = new Centro_Medico
                        {
                            Id = u.empleado.Centro_Medico.Id,
                            Ciudad = u.empleado.Centro_Medico.ciudad,
                            Direccion = u.empleado.Centro_Medico.direccion,
                            Nombre = u.empleado.Centro_Medico.nombre
                        },
                        Especialidad = new Especialidad
                        {
                            Id = u.empleado.Especialidad.Id,
                            Especialidad_ = u.empleado.Especialidad.especialidad
                        },
                        TipoEmpleado = new Tipo_Empleado
                        {
                            Id = u.empleado.Tipo_Empleado.Id,
                            Tipo = u.empleado.Tipo_Empleado.tipo
                        }
                    }
                };
                usuarioLista.Add(usuarioFila);

            }

            return new ListaUsuarios
            {
                Usuarios = { usuarioLista }
            
            }
                ;
        }
        [Authorize(Policy ="TipoEmpleadoPolitica")]
        public override async Task<Usuario> ActualizarUsuario(UsuarioActualizar request, ServerCallContext context)
        {
            var usuarioBuscar = await _context.Usuarios
                .Include(u => u.empleado)
                .FirstOrDefaultAsync(u => u.Id == request.Id);

            if (usuarioBuscar == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El usuario no existe"));
            }

            var empleado = await _context.Empleados
                .Include(e => e.Centro_Medico)
                .Include(e => e.Tipo_Empleado)
                .Include(e => e.Especialidad)
                .FirstOrDefaultAsync(e => e.Id == request.EmpleadoId);

            if (empleado == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "El empleado no existe"));
            }

            // Validar que el empleado no esté asignado a otro usuario
            var empleadoUsuarioBuscar = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.empleadoId == request.EmpleadoId && u.Id != usuarioBuscar.Id);

            if (empleadoUsuarioBuscar != null)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "El empleado ya está asignado a otro usuario"));
            }

            // Actualizar directamente el usuario encontrado
            usuarioBuscar.contraseña = request.Contrasenia;
            usuarioBuscar.empleadoId = request.EmpleadoId;
            usuarioBuscar.nombre_usuario = request.NombreUsuario;
            usuarioBuscar.empleado = empleado;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Error al guardar el usuario."));
            }

            return new Usuario
            {
                Id = usuarioBuscar.Id,
                NombreUsuario = usuarioBuscar.nombre_usuario,
                Contrasenia = usuarioBuscar.contraseña,
                EmpleadoId = usuarioBuscar.empleadoId,
                Empleado = new Empleado
                {
                    Id = empleado.Id,
                    Cedula = empleado.cedula,
                    CentroMedicoID = empleado.centro_medicoID,
                    Email = empleado.email,
                    EspecialidadID = empleado.especialidadID,
                    Nombre = empleado.nombre,
                    Salario = empleado.salario,
                    Telefono = empleado.telefono,
                    TipoEmpleadoID = empleado.tipo_empleadoID,
                    CentroMedico = new Centro_Medico
                    {
                        Id = empleado.Centro_Medico.Id,
                        Ciudad = empleado.Centro_Medico.ciudad,
                        Direccion = empleado.Centro_Medico.direccion,
                        Nombre = empleado.Centro_Medico.nombre
                    },
                    Especialidad = new Especialidad
                    {
                        Id = empleado.Especialidad.Id,
                        Especialidad_ = empleado.Especialidad.especialidad
                    },
                    TipoEmpleado = new Tipo_Empleado
                    {
                        Id = empleado.Tipo_Empleado.Id,
                        Tipo = empleado.Tipo_Empleado.tipo
                    }
                }
            };
        }

    }
}
