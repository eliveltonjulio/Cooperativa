using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class FuncoesController : Controller
{
    private readonly CooperativaDbContext _context;

    public FuncoesController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var funcoes = _context.Funcoes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            funcoes = funcoes.Where(f =>
                f.Nome.Contains(termo) || f.Descricao.Contains(termo) || f.Cbo.Contains(termo));
        }

        var lista = await funcoes.OrderBy(f => f.Nome).ToListAsync();
        ViewBag.Busca = busca;
        return View(lista);
    }

    public IActionResult Create() => View(new Funcao());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Funcao model)
    {
        NormalizarDados(model);
        await ValidarNomeUnicoAsync(model);

        if (!ModelState.IsValid) return View(model);

        model.Id = Guid.NewGuid();
        _context.Funcoes.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Função cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var funcao = await _context.Funcoes.FirstOrDefaultAsync(f => f.Id == id);
        return funcao == null ? NotFound() : View(funcao);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var funcao = await _context.Funcoes.FirstOrDefaultAsync(f => f.Id == id);
        return funcao == null ? NotFound() : View(funcao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Funcao model)
    {
        var funcao = await _context.Funcoes.FirstOrDefaultAsync(f => f.Id == id);
        if (funcao == null) return NotFound();

        NormalizarDados(model);
        await ValidarNomeUnicoAsync(model, id);

        if (!ModelState.IsValid) return View(model);

        funcao.Nome = model.Nome;
        funcao.Descricao = model.Descricao;
        funcao.Cbo = model.Cbo;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Função atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var funcao = await _context.Funcoes.FindAsync(id);
        if (funcao == null)
        {
            return NotFound();
        }

        if (await PossuiRemuneracoesAsync(id))
        {
            // A FK com Restrict impede a exclusão: avisar antes do usuário confirmar.
            TempData["MensagemErro"] = MensagemFuncaoComRemuneracao;
            return RedirectToAction(nameof(Index));
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Funcoes",
            DisplayName = string.IsNullOrWhiteSpace(funcao.Nome) ? funcao.Descricao : funcao.Nome,
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var funcao = await _context.Funcoes.FirstOrDefaultAsync(f => f.Id == id);
        if (funcao == null) return NotFound();

        if (await PossuiRemuneracoesAsync(id))
        {
            // Mesma proteção da validação do GET: a remuneração referência a função (FK Restrict).
            TempData["MensagemErro"] = MensagemFuncaoComRemuneracao;
            return RedirectToAction(nameof(Index));
        }

        _context.Funcoes.Remove(funcao);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Função excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Mensagem exibida quando a função possui remunerações vinculadas (FK Restrict).</summary>
    private const string MensagemFuncaoComRemuneracao =
        "Não é possível excluir a função: existem remunerações vinculadas a ela. Exclua as remunerações antes.";

    /// <summary>Verifica se alguma remuneração referencia a função (impede a exclusão pela FK Restrict).</summary>
    private async Task<bool> PossuiRemuneracoesAsync(Guid funcaoId) =>
        await _context.Remuneracoes.AnyAsync(r => r.FuncaoId == funcaoId);

    /// <summary>
    /// Cada função deve ter um nome único (ignorando maiúsculas/minúsculas e espaços nas pontas).
    /// Em <paramref name="idExclusao"/> informe o id da função que está sendo editada para que
    /// a própria função não seja considerada duplicada.
    /// </summary>
    private async Task ValidarNomeUnicoAsync(Funcao model, Guid? idExclusao = null)
    {
        var nome = (model.Nome ?? string.Empty).Trim();
        if (nome.Length == 0) return;

        var consulta = _context.Funcoes.AsQueryable();
        if (idExclusao.HasValue)
        {
            consulta = consulta.Where(f => f.Id != idExclusao.Value);
        }

        var nomeJaCadastrado = await consulta.AnyAsync(f => f.Nome.Trim().ToLower() == nome.ToLower());
        if (nomeJaCadastrado)
        {
            ModelState.AddModelError(nameof(Funcao.Nome), "Já existe uma função cadastrada com este nome.");
        }
    }

    private static void NormalizarDados(Funcao model)
    {
        model.Nome = (model.Nome ?? string.Empty).Trim();
        model.Descricao = (model.Descricao ?? string.Empty).Trim();
        model.Cbo = FormatarCbo(model.Cbo);
    }

    /// <summary>Normaliza o CBO para o formato 0000-00 (aceita também os 6 dígitos sem hífen).</summary>
    private static string FormatarCbo(string? valor)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length == 6)
        {
            return $"{digitos[..4]}-{digitos[4..]}";
        }

        return (valor ?? string.Empty).Trim();
    }
}
