using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class RegrasHoraExtraController : Controller
{
    private readonly CooperativaDbContext _context;

    public RegrasHoraExtraController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var regras = _context.RegrasHoraExtra.Include(r => r.Contrato).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            regras = regras.Where(r => r.Contrato != null && r.Contrato.EmpresaNome.Contains(termo));
        }

        ViewBag.Busca = busca;
        return View(await regras.OrderBy(r => r.Contrato!.EmpresaNome).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await PopularContratosAsync();
        return View(new RegraHoraExtra());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegraHoraExtra model)
    {
        RemoverErroDeNavegacao();

        if (model.ContratoId.HasValue && await _context.Contratos.AnyAsync(c => c.Id == model.ContratoId.Value) == false)
        {
            ModelState.AddModelError(nameof(model.ContratoId), "Contrato inválido.");
        }

        if (model.ContratoId.HasValue && await _context.RegrasHoraExtra.AnyAsync(r => r.ContratoId == model.ContratoId))
        {
            ModelState.AddModelError(string.Empty, "Já existe uma regra de hora extra cadastrada para este contrato.");
        }

        if (!ModelState.IsValid)
        {
            await PopularContratosAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        _context.RegrasHoraExtra.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Regra de hora extra cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var regra = await _context.RegrasHoraExtra.Include(r => r.Contrato).FirstOrDefaultAsync(r => r.Id == id);
        return regra == null ? NotFound() : View(regra);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var regra = await _context.RegrasHoraExtra.FindAsync(id);
        if (regra == null) return NotFound();

        await PopularContratosAsync();
        return View(regra);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RegraHoraExtra model)
    {
        var regra = await _context.RegrasHoraExtra.FindAsync(id);
        if (regra == null) return NotFound();

        RemoverErroDeNavegacao();

        if (model.ContratoId.HasValue && await _context.Contratos.AnyAsync(c => c.Id == model.ContratoId.Value) == false)
        {
            ModelState.AddModelError(nameof(model.ContratoId), "Contrato inválido.");
        }

        if (model.ContratoId.HasValue && await _context.RegrasHoraExtra.AnyAsync(r => r.ContratoId == model.ContratoId && r.Id != id))
        {
            ModelState.AddModelError(string.Empty, "Já existe uma regra de hora extra cadastrada para este contrato.");
        }

        if (!ModelState.IsValid)
        {
            await PopularContratosAsync();
            return View(model);
        }

        regra.ContratoId = model.ContratoId;
        regra.PercentualHeComum = model.PercentualHeComum;
        regra.PercentualHeDomingoFeriado = model.PercentualHeDomingoFeriado;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Regra de hora extra atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var regra = await _context.RegrasHoraExtra.Include(r => r.Contrato).FirstOrDefaultAsync(r => r.Id == id);
        if (regra == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "RegrasHoraExtra",
            ActionName = "Delete",
            DisplayName = $"Regra para {(regra.Contrato?.EmpresaNome ?? "Geral")}",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var regra = await _context.RegrasHoraExtra.FindAsync(id);
        if (regra == null) return NotFound();

        _context.RegrasHoraExtra.Remove(regra);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Regra de hora extra excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private void RemoverErroDeNavegacao()
    {
        ModelState.Remove(nameof(RegraHoraExtra.Contrato));
    }

    private async Task PopularContratosAsync()
    {
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
    }
}
