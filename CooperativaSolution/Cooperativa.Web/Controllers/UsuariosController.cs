using System.Text.RegularExpressions;
using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

[Authorize(Roles = "Administrador,Admin")]
public class UsuariosController : Controller
{
    private readonly CooperativaDbContext _context;

    public static readonly (string Valor, string Rotulo)[] PerfisDisponiveis =
    {
        ("Administrador", "Administrador (Acesso total)"),
        ("Coordenador", "Coordenador (Gestão de cooperados, alocações e ponto)"),
        ("Cooperado", "Cooperado (Consulta ao próprio cadastro e ponto)")
    };

    public UsuariosController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca, string perfil, string status)
    {
        var usuarios = _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            usuarios = usuarios.Where(u =>
                (u.Login != null && EF.Functions.ILike(u.Login, $"%{termo}%")) ||
                (u.Nome != null && EF.Functions.ILike(u.Nome, $"%{termo}%")) ||
                (u.Email != null && EF.Functions.ILike(u.Email, $"%{termo}%")) ||
                (u.Equipe != null && EF.Functions.ILike(u.Equipe, $"%{termo}%")) ||
                (u.Cargo != null && EF.Functions.ILike(u.Cargo, $"%{termo}%")));
        }

        if (!string.IsNullOrWhiteSpace(perfil) && perfil != "todos")
        {
            usuarios = usuarios.Where(u => u.Perfil == perfil);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "todos")
        {
            var ativo = status == "ativo";
            usuarios = usuarios.Where(u => u.Ativo == ativo);
        }

        var lista = await usuarios.OrderBy(u => u.Nome).ThenBy(u => u.Login).ToListAsync();
        ViewBag.Busca = busca;
        ViewBag.Perfil = perfil;
        ViewBag.Status = status;
        ViewBag.Perfis = PerfisDisponiveis;
        return View(lista);
    }

    public async Task<IActionResult> Create()
    {
        await PopularCombosAsync();
        return View(new Usuario { Ativo = true, Perfil = "Cooperado", DataIngresso = DateTime.Today });
    }

    [HttpGet]
    public async Task<IActionResult> ObterDadosCooperado(Guid id)
    {
        var cooperado = await _context.Cooperados
            .Include(c => c.Contrato)
            .Include(c => c.Funcao)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cooperado == null) return NotFound();

        var celularLimpo = Regex.Replace(cooperado.Telefone ?? string.Empty, @"\D", "");

        return Json(new
        {
            nome = cooperado.Nome,
            email = cooperado.Email,
            celular = celularLimpo,
            contratoId = cooperado.ContratoId,
            contratoNome = cooperado.Contrato?.EmpresaNome,
            cargo = cooperado.Funcao?.Nome,
            equipe = cooperado.Equipe,
            dataIngresso = cooperado.DataAdmissao != default ? cooperado.DataAdmissao.ToString("yyyy-MM-dd") : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Usuario model)
    {
        NormalizarCelular(model);
        model.Perfil = NormalizarPerfil(model.Perfil);

        if (await _context.UsuariosSistema.AnyAsync(u => u.Login.ToLower() == model.Login.Trim().ToLower()))
        {
            ModelState.AddModelError(nameof(model.Login), "Já existe um usuário cadastrado com este login.");
        }

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync(model.Perfil);
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Login = model.Login.Trim();
        model.Email = model.Email.Trim();
        model.Senha = BCrypt.Net.BCrypt.HashPassword(model.Senha);

        if (model.DataIngresso.HasValue)
        {
            model.DataIngresso = DateTime.SpecifyKind(model.DataIngresso.Value.Date, DateTimeKind.Utc);
        }

        _context.UsuariosSistema.Add(model);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Usuário cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var usuario = await _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return NotFound();

        return View(usuario);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var usuario = await _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return NotFound();

        await PopularCombosAsync(usuario.Perfil);
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Usuario model, string? novaSenha)
    {
        var usuario = await _context.UsuariosSistema.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return NotFound();

        NormalizarCelular(model);
        model.Perfil = NormalizarPerfil(model.Perfil);

        if (await _context.UsuariosSistema.AnyAsync(u => u.Login.ToLower() == model.Login.Trim().ToLower() && u.Id != id))
        {
            ModelState.AddModelError(nameof(model.Login), "Já existe outro usuário cadastrado com este login.");
        }

        // Se novaSenha não foi fornecida, remove erro de Senha obrigatória
        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            ModelState.Remove(nameof(Usuario.Senha));
        }

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync(model.Perfil);
            return View(model);
        }

        usuario.Nome = model.Nome;
        usuario.Email = model.Email.Trim();
        usuario.Celular = model.Celular;
        usuario.Login = model.Login.Trim();
        usuario.Perfil = model.Perfil;
        usuario.Ativo = model.Ativo;
        usuario.CooperadoId = model.CooperadoId;
        usuario.ContratoId = model.ContratoId;
        usuario.Cargo = model.Cargo;
        usuario.Equipe = model.Equipe;
        usuario.DataIngresso = model.DataIngresso.HasValue ? DateTime.SpecifyKind(model.DataIngresso.Value.Date, DateTimeKind.Utc) : null;

        if (!string.IsNullOrWhiteSpace(novaSenha))
        {
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
        }

        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Usuário atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var usuario = await _context.UsuariosSistema
            .Include(u => u.Cooperado)
            .Include(u => u.Contrato)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return NotFound();

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Usuarios",
            ActionName = "Delete",
            DisplayName = $"{usuario.Nome} ({usuario.Login})",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var usuario = await _context.UsuariosSistema.FindAsync(id);
        if (usuario == null) return NotFound();

        _context.UsuariosSistema.Remove(usuario);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Usuário excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCombosAsync(string? perfilSelecionado = null)
    {
        ViewBag.PerfisUsuario = new SelectList(PerfisDisponiveis.Select(p => new { Id = p.Valor, Nome = p.Rotulo }), "Id", "Nome", perfilSelecionado);
        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
    }

    private static void NormalizarCelular(Usuario model)
    {
        if (!string.IsNullOrWhiteSpace(model.Celular))
        {
            model.Celular = Regex.Replace(model.Celular, @"\D", "");
        }
    }

    private static string NormalizarPerfil(string perfil)
    {
        if (string.IsNullOrWhiteSpace(perfil)) return "Cooperado";
        if (perfil.Equals("Admin", StringComparison.OrdinalIgnoreCase) || perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            return "Administrador";
        if (perfil.Equals("Coordenador", StringComparison.OrdinalIgnoreCase) || perfil.Equals("Gestor", StringComparison.OrdinalIgnoreCase))
            return "Coordenador";
        return "Cooperado";
    }
}
