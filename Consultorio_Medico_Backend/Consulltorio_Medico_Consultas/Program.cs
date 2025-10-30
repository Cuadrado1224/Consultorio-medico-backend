using Microsoft.EntityFrameworkCore;
using Consulltorio_Medico_Consultas.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Consulltorio_Medico_Consultas.protos;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using MySqlConnector;
using System.Linq;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Leer proveedor desde appsettings: "MariaDb" o "Npgsql" (por defecto Npgsql)
var provider = builder.Configuration["DatabaseProvider"] ?? "Npgsql";

string? mainConnectionString;
string? replicaConnectionString;

if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase))
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
    if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase))
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
        if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase))
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
builder.Services.AddDbContext<DataContext>(options =>
{
    if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase))
    {
        options.UseMySql(workingConnectionString, ServerVersion.AutoDetect(workingConnectionString));
    }
    else
    {
        options.UseNpgsql(workingConnectionString);
    }
});
// Add services to the container.


builder.Services.AddGrpc();
//dotnet dev-certs https --trust
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7230, listenOptions =>
    {
        listenOptions.UseHttps(); // ✅ Usa el certificado por defecto o personalizado
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});


//migracion


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


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    // Verifica si la tabla 'Paciente' existe antes de migrar
    bool pacienteTableExists = false;
    try
    {
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase))
        {
            cmd.CommandText = "SHOW TABLES LIKE 'Paciente'";
            using var reader = cmd.ExecuteReader();
            pacienteTableExists = reader.HasRows;
        }
        else
        {
            cmd.CommandText = "SELECT to_regclass('public.\\\"Paciente\\\"')";
            var result = cmd.ExecuteScalar();
            pacienteTableExists = result != DBNull.Value && result != null;
        }
        conn.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verificando la tabla Paciente: {ex.Message}");
    }
    if (!pacienteTableExists)
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

                if (provider.Equals("MariaDb", StringComparison.OrdinalIgnoreCase) && containsPostgres && !containsMaria)
                {
                    Console.WriteLine("Se detectaron migraciones de Postgres pero el proveedor actual es MariaDB. No se aplicarán migraciones automáticas.");
                }
                else if (provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase) && containsMaria && !containsPostgres)
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
        Console.WriteLine("La tabla 'Paciente' ya existe. No se aplican migraciones automáticas.");
    }
}

app.MapGrpcService<PacienteServiceImpl>();
app.MapGrpcService<ConsultasServiceImpl>();

app.MapGet("/", () => "Comunicacion a trav\u00e9s de GRPC");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
