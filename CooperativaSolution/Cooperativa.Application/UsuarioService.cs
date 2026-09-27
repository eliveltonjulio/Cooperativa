using Cooperativa.Models;

namespace Cooperativa.Application
{
    public class UsuarioService
    {
        public Task<UsuarioSistema?> Autenticar(string login, string senha)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
            {
                return Task.FromResult<UsuarioSistema?>(null);
            }

            if (login.Equals("admin", StringComparison.OrdinalIgnoreCase) && senha == "123456")
            {
                var usuario = new UsuarioSistema
                {
                    Id = Guid.NewGuid(),
                    Login = login,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
                    Perfil = PerfilUsuario.Admin,
                    Ativo = true
                };

                return Task.FromResult<UsuarioSistema?>(usuario);
            }

            return Task.FromResult<UsuarioSistema?>(null);
        }

        public Task<UsuarioSistema> Criar(string nome, string login, string email, string senha, PerfilUsuario perfil)
        {
            var novo = new UsuarioSistema
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Login = login,
                Email = email,
                Perfil = perfil,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
                Ativo = true
            };

            return Task.FromResult(novo);
        }
    }
}