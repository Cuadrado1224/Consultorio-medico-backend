using Consulltorio_Medico_Administracion.Data;
using Consulltorio_Medico_Administracion.Protos;
using Consultorio_Medico_Administracion.Protos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using MySqlConnector;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1 = MariaDB, 2 = PostgreSQL
int nombreBaseDatos = 2; // cambia este valor para alternar

string? mainConnectionString;
string? replicaConnectionString;

if (nombreBaseDatos == 1)
{
    mainConnectionString = builder.Configuration.GetConnectionString("DefaultConnectionMariaDb");
    replicaConnectionString = builder.Configuration.GetConnectionString("ReplicaConnectionMariaDb");
}
else
{
    mainConnectionString = builder.Configuration.GetConnectionString("DefaultConnectionNpgsql");
    replicaConnectionString = builder.Configuration.GetConnectionString("ReplicaConnectionNpgsql");
}

string? workingConnectionString = mainConnectionString;
bool dbAvailable = true;

// Failover: intenta conectar a principal, si falla usa la réplica
try
{
    if (nombreBaseDatos == 1)
    {
        using var conn = new MySqlConnection(mainConnectionString);
        conn.Open();
    }
    else
    {
        using var conn = new NpgsqlConnection(mainConnectionString);
        conn.Open();
    }
    Console.WriteLine("Conexión principal exitosa");
}
catch
{
    try
    {
        if (nombreBaseDatos == 1)
        {
            using var conn = new MySqlConnection(replicaConnectionString);
            conn.Open();
            workingConnectionString = replicaConnectionString;
        }
        else
        {
            using var conn = new NpgsqlConnection(replicaConnectionString);
            conn.Open();
            workingConnectionString = replicaConnectionString;
        }
        Console.WriteLine("Conexión principal fallida, usando réplica");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"No se pudo conectar ni a la principal ni a la réplica: {ex.Message}");
        dbAvailable = false;
    }
}

Console.WriteLine($"Cadena de conexión en uso: {workingConnectionString}");
if (!dbAvailable)
{
    Console.WriteLine("Advertencia: No se pudo conectar a ninguna base de datos. La aplicación arrancará, pero las operaciones que requieran base de datos fallarán.");
}

// Registrar DbContext según proveedor
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (nombreBaseDatos == 1)
    {
        options.UseMySql(workingConnectionString, ServerVersion.AutoDetect(workingConnectionString));
    }
    else
    {
        options.UseNpgsql(workingConnectionString);
    }
});

builder.Services.AddGrpc();
//dotnet dev-certs https --trust
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7256, listenOptions =>
    {
        listenOptions.UseHttps(); // ? Usa el certificado por defecto o personalizado
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

//JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Error de autenticación: {context.Exception}");
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Console.WriteLine($"Error de autenticación: {context.Response}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validado: {context.SecurityToken}");
            return Task.CompletedTask;
        }
    };

    o.RequireHttpsMetadata = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ClockSkew = TimeSpan.Zero
    };

});

builder.Services.AddAuthorization(options =>
    options.AddPolicy("TipoEmpleadoPolitica", policy =>
        policy.RequireAssertion(
                context =>
                context.User.HasClaim("TipoEmpleado", "Administrador") ||
                context.User.HasClaim("TipoEmpleado", "Doctor")
            )
        )
);

// JWT en Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            []
        }
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//migracion
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Verifica si la tabla 'Especialidades' exista antes de migrar
    bool especialidadesTableExists = false;
    try
    {
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Verificar según proveedor
        if (nombreBaseDatos == 1)
        {
            cmd.CommandText = "SHOW TABLES LIKE 'Especialidades'";
            using var reader = cmd.ExecuteReader();
            especialidadesTableExists = reader.HasRows;
        }
        else
        {
            cmd.CommandText = "SELECT to_regclass('public.\\\"Especialidades\\\"')";
            var result = cmd.ExecuteScalar();
            especialidadesTableExists = result != DBNull.Value && result != null;
        }
        conn.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verificando la tabla Especialidades: {ex.Message}");
    }

    // Si no existe la tabla, revisa migraciones pendientes y si son compatibles con el proveedor actual
    if (!especialidadesTableExists)
    {
        try
        {
            var pending = db.Database.GetPendingMigrations().ToList();
            if (!pending.Any())
            {
                Console.WriteLine("No hay migraciones pendientes.");
            }
            else
            {
                bool containsMaria = pending.Any(m => m.IndexOf("Maria", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0);
                bool containsPostgres = pending.Any(m => m.IndexOf("Postgres", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0);

                if (nombreBaseDatos == 1 && containsPostgres && !containsMaria)
                {
                    Console.WriteLine("Se detectaron migraciones de Postgres pero el proveedor actual es MariaDB. No se aplicarán migraciones automáticas.");
                }
                else if (nombreBaseDatos == 2 && containsMaria && !containsPostgres)
                {
                    Console.WriteLine("Se detectaron migraciones de MariaDB pero el proveedor actual es PostgreSQL. No se aplicarán migraciones automáticas.");
                }
                else
                {
                    Console.WriteLine($"Aplicando migraciones pendientes: {string.Join(", ", pending)}");
                    db.Database.Migrate(); // Aplica migraciones solo si parecen compatibles
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al aplicar migraciones: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("La tabla 'Especialidades' ya existe. No se aplican migraciones automáticas.");
    }

    // seed data
    if (!db.Especialidades.Any())
    {
        db.Especialidades.Add(new Consulltorio_Medico_Administracion.Models.Especialidad { Id = 1, especialidad = "Sin Especialidad" });
        db.SaveChanges();
    }
    if (!db.Tipos_Empleados.Any())
    {
        db.Tipos_Empleados.Add(new Consulltorio_Medico_Administracion.Models.Tipo_Empleado { Id = 1, tipo = "Administrador" });
        db.SaveChanges();
    }

    if (!db.Centros_Medicos.Any())
    {
        db.Centros_Medicos.Add(new Consulltorio_Medico_Administracion.Models.Centro_Medico { Id = 1, nombre = "Central", ciudad = "Quito", direccion = "direccion" });
        db.SaveChanges();
    }

    if (!db.Empleados.Any())
    {
        db.Empleados.Add(new Consulltorio_Medico_Administracion.Models.Empleado { Id = 1, nombre = "admin", cedula = "01020304", especialidadID = 1, email = "admin@admin.com", tipo_empleadoID = 1, telefono = "0123456789", centro_medicoID = 1 });
        db.SaveChanges();
    }
    if (!db.Usuarios.Any())
    {
        db.Usuarios.Add(new Consulltorio_Medico_Administracion.Models.Usuario { Id = 1, nombre_usuario = "root", contraseña = "1234", empleadoId = 1 });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGrpcService<UsuarioServiceImpl>();

app.MapGrpcService<AdministracionServiceImpl>();

app.UseAuthorization();

app.MapControllers();

app.Run();
