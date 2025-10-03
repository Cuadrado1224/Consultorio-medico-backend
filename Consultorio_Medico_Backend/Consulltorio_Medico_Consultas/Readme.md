# Microservicio: Consultas Médicas

Servicio gRPC para gestionar consultas médicas de un consultorio. Implementa CRUD de consultas, gestión de pacientes (en este servicio), y consulta de información relacionada a empleados y centros médicos vía microservicio de Administración. Autenticación y autorización mediante JWT. Persistencia con EF Core sobre MariaDB/MySQL.

## Tecnologías y dependencias

- .NET 8 (ASP.NET Core)
- gRPC (Grpc.AspNetCore, Grpc.Net.Client, Grpc.Tools)
- Entity Framework Core + Pomelo.EntityFrameworkCore.MySql
- Autenticación JWT (Microsoft.AspNetCore.Authentication.JwtBearer)
- Kestrel con HTTP/2 + HTTPS
- Docker (opcional) y docker-compose para MariaDB

## Estructura del proyecto

- Controllers/
  - (Web API opcional; la lógica principal está en gRPC)
- Data/
  - DataContext (DbContext EF Core)
- Models/
  - Entidades de dominio (p.ej. `ConsultasMedicasEntity`, `Paciente`)
- Protos/
  - `consultasMedicas.proto` (servicio gRPC de consultas)
  - `paciente.proto` (servicio gRPC de pacientes en este microservicio)
  - `AdministracionService.proto` (cliente gRPC hacia Administración)
  - Implementaciones: `ConsultasServiceImpl.cs`, `PacienteServiceImpl.cs`
- Program.cs
  - Registro de servicios, Kestrel, gRPC, EF, JWT y migraciones
- appsettings.json / appsettings.Development.json
  - Cadenas de conexión, JWT, y URLs de otros microservicios
- Dockerfile, docker-compose.yml
  - Imagen del servicio y DB local

## Configuración

Archivo `appsettings.json` (valores de ejemplo):

```json
{
  "ConnectionStrings": {
    "HospitalConnection": "Server=localhost;Port=3309;Database=consultas;User=root;Password=200232;"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*",
  "grcp": {
    "administracion": "https://localhost:<PUERTO-ADMIN>"
  },
  "Jwt": {
    "Secret": "<clave-secreta-larga>",
    "Audience": "user",
    "Issuer": "Microservicio-Autenticacion",
    "TiempoExpira": 3
  }
}
```

Notas:
- Cambia `<PUERTO-ADMIN>` al puerto donde corre el microservicio de Administración.
- La clave `Jwt:Secret` debe ser segura y no versionarse para producción.
- Si ejecutas múltiples instancias de este servicio (una por centro médico), define en el API Gateway las claves `grcp:centroMedico-<Ciudad>` que apunten a cada instancia.

## Arranque y ejecución

Requisitos:
- .NET 8 SDK
- MariaDB local (opcional si usas docker-compose)
- Certificado de desarrollo HTTPS confiable:
  - Ejecuta una vez: `dotnet dev-certs https --trust`

Arranque de DB con Docker (opcional):
```bash
docker compose up -d
```

Ejecutar el servicio:
```bash
dotnet restore
dotnet build
dotnet run
```

Por defecto, Kestrel escucha con HTTPS/HTTP2 en el puerto 7230 (ver `Program.cs`). En consola deberías ver:
- “Now listening on: https://[::]:7230”
- Mensajes de migraciones EF (las migraciones se aplican automáticamente al iniciar).

## Seguridad (JWT)

- Se configura autenticación JWT con `AddAuthentication().AddJwtBearer(...)`.
- Los métodos gRPC sensibles usan `[Authorize]`.
- El token debe enviarse en metadata de gRPC: `Authorization: Bearer <token>`.

Importante:
- En el pipeline debe estar `UseAuthentication()` antes de `UseAuthorization()`. Verifica tu `Program.cs`.

## API gRPC

Archivo de contrato: `Protos/consultasMedicas.proto`

Servicio: `ConsultasService`

Métodos principales:
- `GetConsultaCedula(ConsultaCedulaRequest) returns (Consulta)`
- `CreateConsulta(CreateConsultaRequest) returns (Consulta)`
- `ActualizarConsulta(UpdateConsultaRequest) returns (Consulta)`
- `DeleteConsulta(DeleteConsultaRequest) returns (EmptyResponse)`
- `GetAllConsultas(EmptyResponse) returns (ConsultaList)`
- `GetConsultasByFecha(ConsultaFechaRequest) returns (ConsultaList)`
- `GetConsultasReporte(ConsultaCedulaRequest) returns (ConsultaList)`
- `GetConsultasCentroMedico(Admin.Centro_MedicoGet) returns (ConsultaList)`

Mensajería relevante:
- `Consulta` incluye datos de empleado (desde Administración), paciente (local) y centro médico (desde Administración).
- Requests incluyen `id_centro_medico` para asociar/enrutar.

Servicio de pacientes en este microservicio:
- `Protos/paciente.proto` con operaciones CRUD y listas, también protegido con `[Authorize]`.

## Llamadas desde otros servicios (microservicio de Administración y Api Gateway)

- Este servicio consulta al microservicio de Administración vía gRPC para:
  - Obtener `Empleado` (médico) por `id_empleado`
  - Obtener `Centro_Medico` por `id_centro_medico`
- El API Gateway resuelve a qué instancia de Consultas llamar según el centro médico (por ciudad o ID), usando claves como `grcp:centroMedico-<Ciudad>`.

## Múltiples instancias por centro médico (multi-tenant simple)

Para ofrecer aislamiento por centro médico:
- Levanta múltiples instancias del mismo binario en puertos distintos (p. ej., 7230, 7231, 7232).
- Usa bases de datos distintas por instancia (e.g., `consultas_cuenca`, `consultas_guayaquil`).
- En el API Gateway configura:
  ```json
  "grcp": {
    "centrosMedicos": [ "centroMedico-Cuenca", "centroMedico-Guayaquil", "centroMedico-Quito" ],
    "centroMedico-Cuenca": "https://localhost:7230",
    "centroMedico-Guayaquil": "https://localhost:7231",
    "centroMedico-Quito": "https://localhost:7232",
    "administracion": "https://localhost:<PUERTO-ADMIN>",
    "autenticacion": "https://localhost:<PUERTO-AUTH>"
  }
  ```
- Alternativa: un único esquema con filtro global por `id_centro_medico` en EF (HasQueryFilter) leyendo el centro desde configuración.

## Migraciones

Las migraciones se aplican automáticamente al iniciar:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.Migrate();
}
```

Para gestionar migraciones manualmente:
```bash
dotnet ef migrations add Init --project Consulltorio_Medico_Consultas
dotnet ef database update --project Consulltorio_Medico_Consultas
```

## Docker

Construir imagen:
```bash
docker build -t consultas-service .
```

Ejecutar (ejemplo simple):
```bash
docker run -p 7230:7230 \
  -e ConnectionStrings__HospitalConnection="Server=<host>;Port=<port>;Database=consultas;User=<user>;Password=<pwd>;" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  consultas-service
```

Con docker-compose (DB incluida), ver `docker-compose.yml` (expone MariaDB en 3309).

## Ejemplos de uso gRPC

Con `grpcurl` (requiere HTTP/2 y cert de desarrollo confiable):

- Listar consultas (añadiendo metadata Authorization):
```bash
grpcurl -insecure \
  -H "Authorization: Bearer <TOKEN>" \
  localhost:7230 consultas.ConsultasService/GetAllConsultas \
  "{}"
```

- Crear consulta:
```bash
grpcurl -insecure \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"fecha":"2025-10-03","hora":"10:00","motivo":"Dolor","diagnostico":"Sin diagnóstico","tratamiento":"Analgésico","id_medico":1,"cedula":"0102030405","id_centro_medico":1}' \
  localhost:7230 consultas.ConsultasService/CreateConsulta
```

Ajusta el nombre completo del servicio `consultas.ConsultasService` según el `package` del .proto (si tu proto no define package, el nombre sería sin prefijo).

## Resolución de problemas (FAQ)

- Error: `System.ArgumentNullException: (Parameter 'uriString')` al crear canal gRPC
  - La URL está llegando `null`. Verifica claves de configuración `grcp:*` y valida antes de `GrpcChannel.ForAddress`.

- Error: `Service 'consultas.ConsultasService' is unimplemented.`
  - Desajuste entre el `package`/nombre del servicio en el .proto del cliente y el del servidor. Usa el mismo .proto en ambos.

- Error DELETE 400/415 desde API Gateway
  - Si tu DELETE espera un body (tipo complejo), envía `Content-Type: application/json` y un JSON no vacío con al menos `idConsultaMedica` e `idCentroMedico`. Si no quieres body, cambia la acción para tomar datos por ruta/query.

- Error `NotFound: El Centro Medico no Existe`
  - El `id_centro_medico` no existe en Administración o el Gateway llama al puerto equivocado de Administración. Evita depender de Administración para operaciones simples (enruta con claims o query).

- gRPC requiere HTTPS/HTTP2
  - Asegura `UseHttps` en Kestrel y confía el cert de desarrollo: `dotnet dev-certs https --trust`.

## Licencia MIT

Este proyecto está bajo la Licencia MIT. Esto significa que puedes usar, copiar, modificar, fusionar, publicar, distribuir, sublicenciar y/o vender copias del software, siempre que incluyas el aviso de copyright original y la declaración de la licencia en cualquier copia sustancial del software.

**Texto completo de la licencia:**

```
MIT License

Copyright (c) [año] [autor]

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```



Proyecto académico/demostración. Ajusta según tu política interna.

---
