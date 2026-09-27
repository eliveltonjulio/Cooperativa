using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Services;

public class UsuarioService
{
    private readonly CooperativaDbContext _context;

    public UsuarioService(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> Autenticar(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            return null;
        }

        var termo = login.Trim().ToLower();
        var usuario = await _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .FirstOrDefaultAsync(u => u.Login.ToLower() == termo || u.Email.ToLower() == termo);

        if (usuario == null || !usuario.Ativo)
        {
            return null;
        }

        bool senhaValida = false;
        try
        {
            senhaValida = BCrypt.Net.BCrypt.Verify(senha, usuario.Senha);
        }
        catch
        {
            // Fallback para compatibilidade se a senha legada estiver em texto puro
            senhaValida = usuario.Senha == senha;
            if (senhaValida)
            {
                usuario.Senha = BCrypt.Net.BCrypt.HashPassword(senha);
                await _context.SaveChangesAsync();
            }
        }

        return senhaValida ? usuario : null;
    }

    public async Task RegistrarUltimoAcesso(Guid usuarioId)
    {
        var usuario = await _context.UsuariosSistema.FindAsync(usuarioId);
        if (usuario != null)
        {
            usuario.UltimoAcesso = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> GerarMfaAsync(Usuario usuario)
    {
        var codigo = Random.Shared.Next(100000, 999999).ToString();
        usuario.MfaCodigoTemp = codigo;
        usuario.MfaCodigoExpiracao = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();
        return codigo;
    }

    public async Task<Usuario?> ValidarMfaAsync(string login, string codigo)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(codigo)) return null;

        var termo = login.Trim().ToLower();
        var usuario = await _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .FirstOrDefaultAsync(u => u.Login.ToLower() == termo || u.Email.ToLower() == termo);

        if (usuario == null || !usuario.Ativo || string.IsNullOrWhiteSpace(usuario.MfaCodigoTemp) || usuario.MfaCodigoExpiracao == null)
        {
            return null;
        }

        if (DateTime.UtcNow > usuario.MfaCodigoExpiracao.Value)
        {
            return null;
        }

        if (usuario.MfaCodigoTemp == codigo.Trim())
        {
            usuario.MfaCodigoTemp = null;
            usuario.MfaCodigoExpiracao = null;
            usuario.UltimoAcesso = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return usuario;
        }

        return null;
    }

    public async Task<string?> SolicitarRedefinicaoSenhaAsync(string loginOuEmail)
    {
        if (string.IsNullOrWhiteSpace(loginOuEmail)) return null;

        var termo = loginOuEmail.Trim().ToLower();
        var usuario = await _context.UsuariosSistema
            .FirstOrDefaultAsync(u => u.Login.ToLower() == termo || u.Email.ToLower() == termo);

        if (usuario == null) return null;

        var token = Guid.NewGuid().ToString("N");
        usuario.TokenRedefinicaoSenha = token;
        usuario.TokenRedefinicaoExpiracao = DateTime.UtcNow.AddHours(2);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<bool> RedefinirSenhaAsync(string token, string novaSenha)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(novaSenha)) return false;

        var usuario = await _context.UsuariosSistema
            .FirstOrDefaultAsync(u => u.TokenRedefinicaoSenha == token.Trim());

        if (usuario == null || usuario.TokenRedefinicaoExpiracao == null || DateTime.UtcNow > usuario.TokenRedefinicaoExpiracao.Value)
        {
            return false;
        }

        usuario.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
        usuario.TokenRedefinicaoSenha = null;
        usuario.TokenRedefinicaoExpiracao = null;
        await _context.SaveChangesAsync();
        return true;
    }
}
