using System;

namespace Cooperativa.Models
{
    public class UsuarioSistema
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Cooperado;
        public bool Ativo { get; set; } = true;
        public Guid? CooperadoId { get; set; }
        public Guid? ContratoId { get; set; }
        public string? Cargo { get; set; }
        public string? Equipe { get; set; }
        public DateTime? DataIngresso { get; set; }
        public DateTime? UltimoAcesso { get; set; }
        public string? MfaCodigoTemp { get; set; }
        public DateTime? MfaCodigoExpiracao { get; set; }
        public string? TokenRedefinicaoSenha { get; set; }
        public DateTime? TokenRedefinicaoExpiracao { get; set; }
    }
}