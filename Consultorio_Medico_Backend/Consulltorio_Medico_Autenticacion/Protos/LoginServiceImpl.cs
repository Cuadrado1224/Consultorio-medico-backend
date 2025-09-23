using Grpc.Core;
using Grpc.Net.Client;
using NuGet.Common;
using Consulltorio_Medico_Autenticacion.Protos;
using Consulltorio_Medico_Autenticacion.Auth;
using Consulltorio_Medico_Administracion.Protos;


namespace Consulltorio_Medico_Autenticacion.Protos
{
    public class LoginServiceImpl : LoginService.LoginServiceBase
    {
        private readonly IConfiguration _config;

        public LoginServiceImpl(IConfiguration config)
        {
            _config = config;
        }
        public override async Task<Token> Login(UsuarioLogin usuario, ServerCallContext context)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var canal = GrpcChannel.ForAddress(_config["grcp:administracion"], new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            var cliente = new UsuarioService.UsuarioServiceClient(canal);


            var respuesta = await cliente.ValidarUsuarioAsync(new UsuarioLogin
            {
                NombreUsuario = usuario.NombreUsuario,
                Contrasenia = usuario.Contrasenia
            });

            if (!respuesta.EsValido && respuesta.Usuario == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Usuario o contraseña Incorrecta"));
            }
            var TokenProvider = new TokenProvider(_config);

            var Token = TokenProvider.Create(respuesta.Usuario);
            return new Protos.Token
            {
                Token_ = Token
            };
        }
    }
}