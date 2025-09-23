# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos del proyecto y restaurar dependencias
COPY *.csproj .
RUN dotnet restore

# Copiar todo y compilar
COPY . .
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Puerto expuesto para gRPC
EXPOSE 7256

# Comando para ejecutar el servicio
ENTRYPOINT ["dotnet", "Microservicio-ConsultasMedicas.dll"]
