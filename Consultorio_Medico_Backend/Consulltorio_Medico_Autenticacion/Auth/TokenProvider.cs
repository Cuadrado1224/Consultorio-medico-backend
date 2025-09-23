using Consulltorio_Medico_Administracion.Protos;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Consulltorio_Medico_Autenticacion.Auth
{
    internal sealed class TokenProvider(IConfiguration configuration)
    {
        public string Create(Usuario usuario)
        {
            string secretKey = configuration["Jwt:Secret"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                new Claim(JwtRegisteredClaimNames.Sub,usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,usuario.NombreUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Empleado.Email.ToString()),
                new Claim("Especialidad",usuario.Empleado.Especialidad.Especialidad_.ToString()),
                new Claim("TipoEmpleado",usuario.Empleado.TipoEmpleado.Tipo.ToString()),
                new Claim("CentroMedico",usuario.Empleado.CentroMedico.Ciudad.ToString()),
                new Claim("IdEmpleado",usuario.Empleado.Id.ToString()),
                new Claim("idCentroMedico",usuario.Empleado.CentroMedicoID.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:TiempoExpira")),
                SigningCredentials = credentials,
                Audience = configuration["Jwt:Audience"],
                Issuer = configuration["Jwt:Issuer"]
            };
            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return token;

        }
    }
}
