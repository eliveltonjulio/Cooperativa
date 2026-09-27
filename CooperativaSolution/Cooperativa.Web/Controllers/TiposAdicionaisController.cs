using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class TiposAdicionaisController : Controller
{
    private readonly CooperativaDbContext _context;

    public TiposAdicionaisController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var tiposAdicionais = _context.TiposAdicionais.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            tiposAdicionais = tiposAdicionais
                .Where(t => t.Nome.Contains(termo) || t.TipoCalculo.Contains(termo));
        }

        var lista = await tiposAdicionais.OrderBy(t => t.Nome).ToListAsync();
        ViewBag.Busca = busca;
        return View(lista);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TipoAdicional model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = Guid.NewGuid();
        _context.TiposAdicionais.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Tipo de adicional cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var tipoAdicional = await _context.TiposAdicionais.FirstOrDefaultAsync(t => t.Id == id);
        return tipoAdicional == null ? NotFound() : View(tipoAdicional);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var tipoAdicional = await _context.TiposAdicionais.FirstOrDefaultAsync(t => t.Id == id);
        return tipoAdicional == null ? NotFound() : View(tipoAdicional);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TipoAdicional model)
    {
        var tipoAdicional = await _context.TiposAdicionais.FirstOrDefaultAsync(t => t.Id == id);
        if (tipoAdicional == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        tipoAdicional.Nome = model.Nome;
        tipoAdicional.TipoCalculo = model.TipoCalculo;
        tipoAdicional.Valor = model.Valor;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Tipo de adicional atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var tipoAdicional = await _context.TiposAdicionais.FirstOrDefaultAsync(t => t.Id == id);
        if (tipoAdicional == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "TiposAdicionais",
            ActionName = "Delete",
            DisplayName = tipoAdicional.Nome,
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var tipoAdicional = await _context.TiposAdicionais.FirstOrDefaultAsync(t => t.Id == id);
        if (tipoAdicional == null)
        {
            return NotFound();
        }

        _context.TiposAdicionais.Remove(tipoAdicional);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Tipo de adicional excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
