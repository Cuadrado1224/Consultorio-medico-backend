using Microsoft.EntityFrameworkCore;
using Consulltorio_Medico_Consultas.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Consulltorio_Medico_Consultas.protos;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string? mainConnectionString = builder.Configuration.GetConnectionString("HospitalConnection");
string? replicaConnectionString = builder.Configuration.GetConnectionString("HospitalReplica");
string? workingConnectionString = mainConnectionString;
bool dbAvailable = true;

// Failover: intenta conectar a principal, si falla usa la réplica
try
{
    using var conn = new NpgsqlConnection(mainConnectionString);
    conn.Open();
    Console.WriteLine("Conexión principal exitosa");
}
catch
{
    try
    {
        using var conn = new NpgsqlConnection(replicaConnectionString);
        conn.Open();
        workingConnectionString = replicaConnectionString;
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

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(workingConnectionString));
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
        cmd.CommandText = "SELECT to_regclass('public.\"Paciente\"')";
        var result = cmd.ExecuteScalar();
        pacienteTableExists = result != DBNull.Value && result != null;
        conn.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verificando la tabla Paciente: {ex.Message}");
    }
    if (!pacienteTableExists)
    {
        db.Database.Migrate(); // Aplica migraciones solo si la tabla no existe
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
