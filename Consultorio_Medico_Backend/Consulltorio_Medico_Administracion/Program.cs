using Microservicio_Administracion.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microservicio_Administracion.Models;
using Microservicio_Administracion.Protos;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($" Cadena de conexión: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(
    options => {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    );

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
    //c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tu API", Version = "v1" });
    //c.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));
    // Configuración de seguridad JWT
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
    db.Database.Migrate(); // <-- esto aplica las migraciones
    if (!db.Especialidades.Any())
    {
        db.Especialidades.Add(new Microservicio_Administracion.Models.Especialidad { Id = 1, especialidad = "Sin Especialidad" });
        db.SaveChanges();
    }
    if (!db.Tipos_Empleados.Any())
    {
        db.Tipos_Empleados.Add(new Microservicio_Administracion.Models.Tipo_Empleado { Id = 1, tipo = "Administrador" });
        db.SaveChanges();
    }

    if (!db.Centros_Medicos.Any())
    {
        db.Centros_Medicos.Add(new Microservicio_Administracion.Models.Centro_Medico { Id = 1, nombre = "Central", ciudad = "Quito", direccion = "direccion" });
        db.SaveChanges();
    }

    if (!db.Empleados.Any())
    {
        db.Empleados.Add(new Microservicio_Administracion.Models.Empleado { Id = 1, nombre = "admin", cedula = "01020304", especialidadID = 1, email = "admin@admin.com", tipo_empleadoID = 1, telefono = "0123456789", centro_medicoID = 1 });
        db.SaveChanges();
    }
    if (!db.Usuarios.Any())
    {
        db.Usuarios.Add(new Microservicio_Administracion.Models.Usuario { Id = 1, nombre_usuario = "root", contraseña = "1234", empleadoId = 1 });
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
