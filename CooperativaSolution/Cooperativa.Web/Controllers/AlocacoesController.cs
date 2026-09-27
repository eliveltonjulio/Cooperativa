using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class AlocacoesController : Controller
{
    private readonly CooperativaDbContext _context;

    public AlocacoesController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var alocacoes = _context.Alocacoes
            .Include(a => a.Cooperado)
            .Include(a => a.Contrato)
            .Include(a => a.Funcao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            alocacoes = alocacoes.Where(a => (a.Cooperado != null && EF.Functions.ILike(a.Cooperado.Nome, $"%{termo}%"))
                || (a.Contrato != null && EF.Functions.ILike(a.Contrato.EmpresaNome, $"%{termo}%"))
                || (a.Funcao != null && EF.Functions.ILike(a.Funcao.Nome, $"%{termo}%")));
        }

        var lista = await alocacoes.OrderByDescending(a => a.DataInicio).ToListAsync();
        ViewBag.Busca = busca;
        return View(lista);
    }

    public async Task<IActionResult> Create()
    {
        if (User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor"))
        {
            TempData["MensagemErro"] = "Cooperados possuem permissão apenas para consulta de alocações.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
        ViewBag.Funcoes = await _context.Funcoes.OrderBy(f => f.Nome).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Alocacao model)
    {
        if (User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor"))
        {
            TempData["MensagemErro"] = "Apenas Administradores e Coordenadores podem cadastrar alocações.";
            return RedirectToAction(nameof(Index));
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
            ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
            ViewBag.Funcoes = await _context.Funcoes.OrderBy(f => f.Nome).ToListAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.DataInicio = DateTime.SpecifyKind(model.DataInicio, DateTimeKind.Utc);
        if (model.DataFim.HasValue)
        {
            model.DataFim = DateTime.SpecifyKind(model.DataFim.Value, DateTimeKind.Utc);
        }

        _context.Alocacoes.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Alocação cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var alocacao = await _context.Alocacoes
            .Include(a => a.Cooperado)
            .Include(a => a.Contrato)
            .Include(a => a.Funcao)
            .FirstOrDefaultAsync(a => a.Id == id);
        return alocacao == null ? NotFound() : View(alocacao);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        if (User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor"))
        {
            TempData["MensagemErro"] = "Cooperados possuem permissão apenas para consulta.";
            return RedirectToAction(nameof(Index));
        }

        var alocacao = await _context.Alocacoes.FirstOrDefaultAsync(a => a.Id == id);
        if (alocacao == null) return NotFound();

        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
        ViewBag.Funcoes = await _context.Funcoes.OrderBy(f => f.Nome).ToListAsync();
        return View(alocacao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Alocacao model)
    {
        if (User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor"))
        {
            TempData["MensagemErro"] = "Apenas Administradores e Coordenadores podem editar alocações.";
            return RedirectToAction(nameof(Index));
        }

        var alocacao = await _context.Alocacoes.FirstOrDefaultAsync(a => a.Id == id);
        if (alocacao == null) return NotFound();

        alocacao.CooperadoId = model.CooperadoId;
        alocacao.ContratoId = model.ContratoId;
        alocacao.FuncaoId = model.FuncaoId;
        alocacao.DataInicio = DateTime.SpecifyKind(model.DataInicio, DateTimeKind.Utc);
        alocacao.DataFim = model.DataFim.HasValue ? DateTime.SpecifyKind(model.DataFim.Value, DateTimeKind.Utc) : null;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Alocação atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var alocacao = await _context.Alocacoes
            .Include(a => a.Cooperado)
            .Include(a => a.Contrato)
            .Include(a => a.Funcao)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (alocacao == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Alocacoes",
            DisplayName = $"Alocação de {alocacao.Cooperado?.Nome} no contrato {alocacao.Contrato?.EmpresaNome}",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var alocacao = await _context.Alocacoes.FirstOrDefaultAsync(a => a.Id == id);
        if (alocacao == null) return NotFound();

        _context.Alocacoes.Remove(alocacao);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Alocação excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
