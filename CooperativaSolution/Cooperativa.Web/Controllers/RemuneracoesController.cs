using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class RemuneracoesController : Controller
{
    private readonly CooperativaDbContext _context;

    public RemuneracoesController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var remuneracoes = _context.Remuneracoes
            .Include(r => r.Contrato)
            .Include(r => r.Funcao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            remuneracoes = remuneracoes.Where(r =>
                (r.Contrato != null && EF.Functions.ILike(r.Contrato.EmpresaNome, $"%{termo}%"))
                || (r.Funcao != null && EF.Functions.ILike(r.Funcao.Nome, $"%{termo}%")));
        }

        var lista = await remuneracoes.ToListAsync();
        ViewBag.Busca = busca;
        return View(lista);
    }

    public async Task<IActionResult> Create()
    {
        await PopularCombosAsync();
        return View(new Remuneracao { TipoRemuneracao = "DIA" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Remuneracao model)
    {
        NormalizarValor(model);
        RemoverErrosDeNavegacao();
        await ValidarSelecoesAsync(model);

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        _context.Remuneracoes.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Remuneração cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var remuneracao = await _context.Remuneracoes
            .Include(r => r.Contrato)
            .Include(r => r.Funcao)
            .FirstOrDefaultAsync(r => r.Id == id);
        return remuneracao == null ? NotFound() : View(remuneracao);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var remuneracao = await _context.Remuneracoes.FirstOrDefaultAsync(r => r.Id == id);
        if (remuneracao == null) return NotFound();

        remuneracao.ValorTexto = ValorMonetario.Formatar(remuneracao.Valor);
        await PopularCombosAsync();
        return View(remuneracao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Remuneracao model)
    {
        var remuneracao = await _context.Remuneracoes.FirstOrDefaultAsync(r => r.Id == id);
        if (remuneracao == null) return NotFound();

        NormalizarValor(model);
        RemoverErrosDeNavegacao();
        await ValidarSelecoesAsync(model);

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        remuneracao.ContratoId = model.ContratoId;
        remuneracao.FuncaoId = model.FuncaoId;
        remuneracao.TipoRemuneracao = model.TipoRemuneracao;
        remuneracao.Valor = model.Valor;
        remuneracao.DataInicio = model.DataInicio;
        remuneracao.DataFim = model.DataFim;
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Remuneração atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var remuneracao = await _context.Remuneracoes
            .Include(r => r.Contrato)
            .Include(r => r.Funcao)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (remuneracao == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Remuneracoes",
            ActionName = "Delete",
            DisplayName = $"Remuneração ({remuneracao.Contrato?.EmpresaNome} - {remuneracao.Funcao?.Nome})",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var remuneracao = await _context.Remuneracoes.FirstOrDefaultAsync(r => r.Id == id);
        if (remuneracao == null) return NotFound();

        _context.Remuneracoes.Remove(remuneracao);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Remuneração excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCombosAsync()
    {
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
        ViewBag.Funcoes = await _context.Funcoes.OrderBy(f => f.Nome).ToListAsync();
    }

    private void RemoverErrosDeNavegacao()
    {
        ModelState.Remove(nameof(Remuneracao.Contrato));
        ModelState.Remove(nameof(Remuneracao.Funcao));
    }

    /// <summary>
    /// Converte o valor digitado livremente (ex.: "1.500,50" ou "1500.50") para
    /// <see cref="Remuneracao.Valor"/>, evitando a leitura incorreta do separador decimal
    /// pela cultura pt-BR do binding (onde "1500.50" viraria 150050,00).
    /// </summary>
    private void NormalizarValor(Remuneracao model)
    {
        ModelState.Remove(nameof(Remuneracao.Valor));
        ModelState.Remove(nameof(Remuneracao.ValorTexto));

        if (string.IsNullOrWhiteSpace(model.ValorTexto))
        {
            ModelState.AddModelError(nameof(model.ValorTexto), "Informe o valor da remuneração.");
            return;
        }

        if (!ValorMonetario.TentarConverter(model.ValorTexto, out var valor))
        {
            ModelState.AddModelError(nameof(model.ValorTexto), "Informe o valor da remuneração em um formato válido (ex.: 1.500,50).");
            return;
        }

        if (valor is < 0m or > 100000000m)
        {
            ModelState.AddModelError(nameof(model.ValorTexto), "O valor da remuneração deve estar entre 0,00 e 100.000.000,00.");
            return;
        }

        model.Valor = valor;
    }

    private async Task ValidarSelecoesAsync(Remuneracao model)
    {
        if (model.ContratoId == Guid.Empty || !await _context.Contratos.AnyAsync(c => c.Id == model.ContratoId))
        {
            ModelState.AddModelError(nameof(model.ContratoId), "Selecione um contrato.");
        }

        if (model.FuncaoId == Guid.Empty || !await _context.Funcoes.AnyAsync(f => f.Id == model.FuncaoId))
        {
            ModelState.AddModelError(nameof(model.FuncaoId), "Selecione uma função.");
        }

        if (model.DataInicio == default)
        {
            ModelState.AddModelError(nameof(model.DataInicio), "Informe a data de início.");
        }

        // A data fim é opcional: sem ela a remuneração permanece vigente.
        if (model.DataFim.HasValue && model.DataFim.Value.Date < model.DataInicio.Date)
        {
            ModelState.AddModelError(nameof(model.DataFim), "A data fim deve ser posterior ou igual à data de início.");
        }

        if (model.TipoRemuneracao != "DIA" && model.TipoRemuneracao != "HORA")
        {
            ModelState.AddModelError(nameof(model.TipoRemuneracao), "Selecione o tipo de remuneração (dia ou hora).");
        }
    }
}