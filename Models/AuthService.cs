using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cooperativa.Data;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Models
{
    public class AuthService
{
    private readonly CooperativaDbContext _context;

    public AuthService(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<UsuarioSistema?> AutenticarAsync(string login, string senha)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

        if (usuario == null) return null;

        bool valido = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
        return valido ? usuario : null;
    }

    public async Task<UsuarioSistema> CriarUsuarioAsync(string nome, string login, string email, string senha, PerfilUsuario perfil)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(senha);

        var usuario = new UsuarioSistema
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Login = login,
            Email = email,
            SenhaHash = hash,
            Perfil = perfil,
            Ativo = true
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
}

}