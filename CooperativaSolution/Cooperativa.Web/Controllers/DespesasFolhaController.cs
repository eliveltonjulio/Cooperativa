using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class DespesasFolhaController : Controller
{
    private readonly CooperativaDbContext _context;

    public DespesasFolhaController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca, string status = "ativos")
    {
        var despesas = _context.DespesasFolha.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            despesas = despesas.Where(d => d.Nome.Contains(termo) || d.TipoCalculo.Contains(termo));
        }

        if (status == "ativos") despesas = despesas.Where(d => d.Ativo);
        if (status == "inativos") despesas = despesas.Where(d => !d.Ativo);

        ViewBag.Busca = busca;
        ViewBag.Status = status;
        return View(await despesas.OrderBy(d => d.Nome).ToListAsync());
    }

    public IActionResult Create() => View(new DespesaFolha { TipoCalculo = "PERCENTUAL_REMUNERACAO" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DespesaFolha model)
    {
        if (model.TipoCalculo == "VALOR_FIXO") model.TetoRetencao = null;
        if (!ModelState.IsValid) return View(model);

        model.Id = Guid.NewGuid();
        _context.DespesasFolha.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Despesa da folha cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var despesa = await _context.DespesasFolha.FindAsync(id);
        return despesa == null ? NotFound() : View(despesa);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var despesa = await _context.DespesasFolha.FindAsync(id);
        return despesa == null ? NotFound() : View(despesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DespesaFolha model)
    {
        var despesa = await _context.DespesasFolha.FindAsync(id);
        if (despesa == null) return NotFound();

        if (model.TipoCalculo == "VALOR_FIXO") model.TetoRetencao = null;
        if (!ModelState.IsValid) return View(model);

        despesa.Nome = model.Nome;
        despesa.TipoCalculo = model.TipoCalculo;
        despesa.Valor = model.Valor;
        despesa.TetoRetencao = model.TetoRetencao;
        despesa.Ativo = model.Ativo;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Despesa da folha atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var despesa = await _context.DespesasFolha.FindAsync(id);
        if (despesa == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "DespesasFolha",
            ActionName = "Delete",
            DisplayName = despesa.Nome,
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var despesa = await _context.DespesasFolha.FindAsync(id);
        if (despesa == null) return NotFound();

        _context.DespesasFolha.Remove(despesa);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Despesa da folha excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
