using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class ApuracoesPontoMensaisController : Controller
{
    private readonly CooperativaDbContext _context;

    public ApuracoesPontoMensaisController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca, DateTime? mesAno)
    {
        var apuracoes = _context.ApuracoesPontoMensais
            .Include(a => a.Cooperado)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            apuracoes = apuracoes.Where(a => a.Cooperado != null && EF.Functions.ILike(a.Cooperado.Nome, $"%{termo}%"));
        }

        if (mesAno.HasValue)
        {
            var competencia = PrimeiroDiaDoMes(mesAno.Value);
            apuracoes = apuracoes.Where(a => a.MesAno == competencia);
        }

        ViewBag.Busca = busca;
        ViewBag.MesAno = mesAno?.ToString("yyyy-MM");
        return View(await apuracoes.OrderByDescending(a => a.MesAno).ThenBy(a => a.Cooperado!.Nome).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await PopularCooperadosAsync();
        return View(new ApuracaoPontoMensal { MesAno = PrimeiroDiaDoMes(DateTime.UtcNow) });
    }

    [HttpGet]
    public async Task<IActionResult> Totais(Guid cooperadoId, DateTime mesAno)
    {
        if (cooperadoId == Guid.Empty || mesAno == default)
        {
            return BadRequest();
        }

        var totais = await CalcularTotaisAsync(cooperadoId, PrimeiroDiaDoMes(mesAno));
        return Json(totais);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApuracaoPontoMensal model)
    {
        RemoverErroDeNavegacao();
        model.MesAno = PrimeiroDiaDoMes(model.MesAno);
        await ValidarModelAsync(model);

        if (await ExisteAsync(model.CooperadoId, model.MesAno))
        {
            ModelState.AddModelError(string.Empty, "Já existe uma apuração para este cooperado e mês.");
        }

        if (!ModelState.IsValid)
        {
            await PopularCooperadosAsync();
            return View(model);
        }

        var totais = await CalcularTotaisAsync(model.CooperadoId, model.MesAno);
        model.QtdHorasNormais = totais.HorasNormais;
        model.QtdHorasExtras = totais.HorasExtras;
        model.QtdHorasNoturnas = totais.HorasNoturnas;
        _context.ApuracoesPontoMensais.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Apuração mensal cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid cooperadoId, DateTime mesAno)
    {
        var apuracao = await BuscarAsync(cooperadoId, mesAno);
        return apuracao == null ? NotFound() : View(apuracao);
    }

    public async Task<IActionResult> Edit(Guid cooperadoId, DateTime mesAno)
    {
        var apuracao = await BuscarAsync(cooperadoId, mesAno);
        if (apuracao == null) return NotFound();

        await PopularCooperadosAsync();
        return View(apuracao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid cooperadoIdOriginal, DateTime mesAnoOriginal, ApuracaoPontoMensal model)
    {
        var apuracao = await BuscarAsync(cooperadoIdOriginal, mesAnoOriginal);
        if (apuracao == null) return NotFound();

        RemoverErroDeNavegacao();
        model.MesAno = PrimeiroDiaDoMes(model.MesAno);
        await ValidarModelAsync(model);

        var chaveAlterada = model.CooperadoId != cooperadoIdOriginal || model.MesAno != PrimeiroDiaDoMes(mesAnoOriginal);
        if (chaveAlterada && await ExisteAsync(model.CooperadoId, model.MesAno))
        {
            ModelState.AddModelError(string.Empty, "Já existe uma apuração para este cooperado e mês.");
        }

        if (!ModelState.IsValid)
        {
            await PopularCooperadosAsync();
            return View(model);
        }

        if (chaveAlterada)
        {
            await _context.ApuracoesPontoMensais
                .Where(a => a.CooperadoId == cooperadoIdOriginal && a.MesAno == mesAnoOriginal)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.CooperadoId, model.CooperadoId)
                    .SetProperty(a => a.MesAno, model.MesAno)
                    .SetProperty(a => a.QtdHorasNormais, model.QtdHorasNormais)
                    .SetProperty(a => a.QtdHorasExtras, model.QtdHorasExtras)
                    .SetProperty(a => a.QtdHorasNoturnas, model.QtdHorasNoturnas));
        }
        else
        {
            apuracao.QtdHorasNormais = model.QtdHorasNormais;
            apuracao.QtdHorasExtras = model.QtdHorasExtras;
            apuracao.QtdHorasNoturnas = model.QtdHorasNoturnas;
            await _context.SaveChangesAsync();
        }

        TempData["MensagemSucesso"] = "Apuração mensal atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid cooperadoId, DateTime mesAno)
    {
        var apuracao = await BuscarAsync(cooperadoId, mesAno);
        if (apuracao == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "ApuracoesPontoMensais",
            DisplayName = $"{apuracao.Cooperado?.Nome} - {apuracao.MesAno:MM/yyyy}",
            RouteValues = new() {
                { "cooperadoId", cooperadoId },
                { "mesAno", mesAno }
            }
        };

        return PartialView("_DeleteConfirmation", model);    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid cooperadoId, DateTime mesAno)
    {
        var apuracao = await _context.ApuracoesPontoMensais.FirstOrDefaultAsync(a =>
            a.CooperadoId == cooperadoId && a.MesAno == PrimeiroDiaDoMes(mesAno));
        if (apuracao == null) return NotFound();

        _context.ApuracoesPontoMensais.Remove(apuracao);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Apuração mensal excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApuracaoPontoMensal?> BuscarAsync(Guid cooperadoId, DateTime mesAno)
    {
        return await _context.ApuracoesPontoMensais
            .Include(a => a.Cooperado)
            .FirstOrDefaultAsync(a => a.CooperadoId == cooperadoId && a.MesAno == PrimeiroDiaDoMes(mesAno));
    }

    private async Task<bool> ExisteAsync(Guid cooperadoId, DateTime mesAno)
    {
        return await _context.ApuracoesPontoMensais.AnyAsync(a =>
            a.CooperadoId == cooperadoId && a.MesAno == mesAno);
    }

    private async Task<TotaisApuracaoPonto> CalcularTotaisAsync(Guid cooperadoId, DateTime mesAno)
    {
        var fimDoMes = mesAno.AddMonths(1);
        var registros = await _context.RegistrosPonto
            .Where(registro => registro.CooperadoId == cooperadoId
                && registro.Data >= mesAno
                && registro.Data < fimDoMes)
            .ToListAsync();
        var alocacoes = await _context.Alocacoes
            .Include(alocacao => alocacao.Contrato)
            .Where(alocacao => alocacao.CooperadoId == cooperadoId
                && alocacao.DataInicio < fimDoMes
                && (alocacao.DataFim == null || alocacao.DataFim >= mesAno))
            .ToListAsync();
        var jornadas = await _context.JornadasContratuais.ToListAsync();
        var configuracaoNoturna = await _context.ConfiguracoesHoraNoturna.FirstOrDefaultAsync();
        var feriados = await _context.Feriados
            .Where(feriado => feriado.Data >= mesAno && feriado.Data < fimDoMes)
            .ToListAsync();

        return CalculadoraApuracaoPontoMensal.Calcular(registros, alocacoes, jornadas, configuracaoNoturna, feriados);
    }

    private async Task ValidarModelAsync(ApuracaoPontoMensal model)
    {
        if (model.CooperadoId == Guid.Empty || !await _context.Cooperados.AnyAsync(c => c.Id == model.CooperadoId))
        {
            ModelState.AddModelError(nameof(model.CooperadoId), "Selecione um cooperado.");
        }
    }

    private async Task PopularCooperadosAsync()
    {
        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
    }

    private void RemoverErroDeNavegacao()
    {
        ModelState.Remove(nameof(ApuracaoPontoMensal.Cooperado));
    }

    private static DateTime PrimeiroDiaDoMes(DateTime data)
    {
        return new DateTime(data.Year, data.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
