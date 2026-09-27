using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class FeriadosController : Controller
{
    private static readonly (int Mes, int Dia, string Descricao)[] FeriadosNacionaisPermanentes =
    {
        (1, 1, "Confraternização Universal"),
        (4, 21, "Tiradentes"),
        (5, 1, "Dia Mundial do Trabalho"),
        (9, 7, "Independência do Brasil"),
        (10, 12, "Nossa Senhora Aparecida"),
        (11, 2, "Finados"),
        (11, 15, "Proclamação da República"),
        (11, 20, "Dia Nacional de Zumbi e da Consciência Negra"),
        (12, 25, "Natal")
    };

    private readonly CooperativaDbContext _context;

    public FeriadosController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca, string tipo)
    {
        var feriados = _context.Feriados.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            feriados = feriados.Where(f => f.Descricao.Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "todos")
        {
            feriados = feriados.Where(f => f.Tipo == tipo);
        }

        ViewBag.Busca = busca;
        ViewBag.Tipo = tipo;
        return View(await feriados.OrderBy(f => f.Data).ToListAsync());
    }

    public IActionResult Create() => View(new Feriado { Tipo = "NACIONAL" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IncluirNacionaisPermanentes(int ano)
    {
        if (ano is < 1900 or > 9999)
        {
            TempData["MensagemErro"] = "Informe um ano válido para incluir os feriados nacionais.";
            return RedirectToAction(nameof(Index));
        }

        var inicioAno = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimAno = inicioAno.AddYears(1);
        var datasExistentes = await _context.Feriados
            .Where(feriado => feriado.Data >= inicioAno && feriado.Data < fimAno)
            .Select(feriado => feriado.Data)
            .ToListAsync();

        var novosFeriados = FeriadosNacionaisPermanentes
            .Select(feriado => new Feriado
            {
                Id = Guid.NewGuid(),
                Data = new DateTime(ano, feriado.Mes, feriado.Dia, 0, 0, 0, DateTimeKind.Utc),
                Descricao = feriado.Descricao,
                Tipo = "NACIONAL"
            })
            .Where(feriado => !datasExistentes.Contains(feriado.Data))
            .ToList();

        if (novosFeriados.Count == 0)
        {
            TempData["MensagemSucesso"] = "Todos os feriados nacionais permanentes desse ano já estão cadastrados.";
        }
        else
        {
            _context.Feriados.AddRange(novosFeriados);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = $"{novosFeriados.Count} feriado(s) nacional(is) permanente(s) incluído(s).";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Feriado model)
    {
        model.Data = DateTime.SpecifyKind(model.Data.Date, DateTimeKind.Utc);

        if (await _context.Feriados.AnyAsync(f => f.Data == model.Data))
        {
            ModelState.AddModelError(nameof(model.Data), "Já existe um feriado cadastrado nesta data.");
        }

        if (!ModelState.IsValid) return View(model);

        model.Id = Guid.NewGuid();
        _context.Feriados.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Feriado cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        return feriado == null ? NotFound() : View(feriado);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        return feriado == null ? NotFound() : View(feriado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Feriado model)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        if (feriado == null) return NotFound();

        model.Data = DateTime.SpecifyKind(model.Data.Date, DateTimeKind.Utc);

        if (await _context.Feriados.AnyAsync(f => f.Data == model.Data && f.Id != id))
        {
            ModelState.AddModelError(nameof(model.Data), "Já existe um feriado cadastrado nesta data.");
        }

        if (!ModelState.IsValid) return View(model);

        feriado.Data = model.Data;
        feriado.Descricao = model.Descricao;
        feriado.Tipo = model.Tipo;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Feriado atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        if (feriado == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Feriados",
            ActionName = "Delete",
            DisplayName = $"{feriado.Descricao} ({feriado.Data:dd/MM/yyyy})",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        if (feriado == null) return NotFound();

        _context.Feriados.Remove(feriado);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Feriado excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
