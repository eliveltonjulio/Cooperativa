using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class ContratoFuncaoAdicionaisController : Controller
{
    private readonly CooperativaDbContext _context;

    public ContratoFuncaoAdicionaisController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var vinculos = _context.ContratosFuncoesAdicionais
            .Include(v => v.Contrato)
            .Include(v => v.Funcao)
            .Include(v => v.Adicional)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            // Usando EF.Functions.ILike para busca case-insensitive otimizada para PostgreSQL
            vinculos = vinculos.Where(v => (v.Contrato != null && EF.Functions.ILike(v.Contrato.EmpresaNome, $"%{termo}%"))
                || (v.Funcao != null && EF.Functions.ILike(v.Funcao.Nome, $"%{termo}%"))
                || (v.Adicional != null && EF.Functions.ILike(v.Adicional.Nome, $"%{termo}%")));
        }

        var lista = await vinculos
            .OrderBy(v => v.Contrato!.EmpresaNome)
            .ThenBy(v => v.Funcao!.Nome)
            .ThenBy(v => v.Adicional!.Nome)
            .ToListAsync();

        ViewBag.Busca = busca;
        return View(lista);
    }

    public async Task<IActionResult> Create()
    {
        await PopularCombosAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContratoFuncaoAdicional model)
    {
        RemoverErrosDeNavegacao();
        await ValidarSelecoesAsync(model);

        var duplicado = await _context.ContratosFuncoesAdicionais.AnyAsync(v =>
            v.ContratoId == model.ContratoId
            && v.FuncaoId == model.FuncaoId
            && v.AdicionalId == model.AdicionalId);

        if (duplicado)
        {
            ModelState.AddModelError(string.Empty, "Este adicional já está vinculado ao contrato e função selecionados.");
        }

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        _context.ContratosFuncoesAdicionais.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Adicional vinculado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid contratoId, Guid funcaoId, Guid adicionalId)
    {
        var vinculo = await BuscarVinculoAsync(contratoId, funcaoId, adicionalId);
        return vinculo == null ? NotFound() : View(vinculo);
    }

    public async Task<IActionResult> Edit(Guid contratoId, Guid funcaoId, Guid adicionalId)
    {
        var vinculo = await BuscarVinculoAsync(contratoId, funcaoId, adicionalId);
        if (vinculo == null)
        {
            return NotFound();
        }
        await PopularCombosAsync();
        return View(vinculo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid contratoIdOriginal,
        Guid funcaoIdOriginal,
        Guid adicionalIdOriginal,
        ContratoFuncaoAdicional model)
    {
        var vinculo = await BuscarVinculoAsync(contratoIdOriginal, funcaoIdOriginal, adicionalIdOriginal);
        if (vinculo == null)
        {
            return NotFound();
        }

        RemoverErrosDeNavegacao();
        await ValidarSelecoesAsync(model);

        var combinacaoAlterada = model.ContratoId != contratoIdOriginal
            || model.FuncaoId != funcaoIdOriginal
            || model.AdicionalId != adicionalIdOriginal;

        if (combinacaoAlterada)
        {
            var duplicado = await _context.ContratosFuncoesAdicionais.AnyAsync(v =>
                v.ContratoId == model.ContratoId
                && v.FuncaoId == model.FuncaoId
                && v.AdicionalId == model.AdicionalId);

            if (duplicado)
            {
                ModelState.AddModelError(string.Empty, "Este adicional já está vinculado ao contrato e função selecionados.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        if (combinacaoAlterada)
        {
            // A chave primária é composta: remove o vínculo antigo e cadastra o novo.
            _context.ContratosFuncoesAdicionais.Remove(vinculo);
            _context.ContratosFuncoesAdicionais.Add(new ContratoFuncaoAdicional
            {
                ContratoId = model.ContratoId,
                FuncaoId = model.FuncaoId,
                AdicionalId = model.AdicionalId
            });
            await _context.SaveChangesAsync();
        }

        TempData["MensagemSucesso"] = "Vínculo de adicional atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> Delete(Guid contratoId, Guid funcaoId, Guid adicionalId)
    {
        var vinculo = await BuscarVinculoAsync(contratoId, funcaoId, adicionalId);
        if (vinculo == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "ContratoFuncaoAdicionais",
            DisplayName = $"{vinculo.Contrato?.EmpresaNome ?? "Contrato Inválido"} - {vinculo.Funcao?.Nome ?? "Função Inválida"} - {vinculo.Adicional?.Nome ?? "Adicional Inválido"}",
            RouteValues = new() {
                { "contratoId", contratoId },
                { "funcaoId", funcaoId },
                { "adicionalId", adicionalId }
            }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid contratoId, Guid funcaoId, Guid adicionalId)
    {
        var vinculo = await _context.ContratosFuncoesAdicionais.FirstOrDefaultAsync(v =>
            v.ContratoId == contratoId
            && v.FuncaoId == funcaoId
            && v.AdicionalId == adicionalId);

        if (vinculo == null)
        {
            return NotFound();
        }

        _context.ContratosFuncoesAdicionais.Remove(vinculo);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Vínculo de adicional excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ContratoFuncaoAdicional?> BuscarVinculoAsync(Guid contratoId, Guid funcaoId, Guid adicionalId)
    {
        return await _context.ContratosFuncoesAdicionais
            .Include(v => v.Contrato)
            .Include(v => v.Funcao)
            .Include(v => v.Adicional)
            .FirstOrDefaultAsync(v => v.ContratoId == contratoId
                && v.FuncaoId == funcaoId
                && v.AdicionalId == adicionalId);
    }

    private async Task ValidarSelecoesAsync(ContratoFuncaoAdicional model)
    {
        if (model.ContratoId == Guid.Empty || !await _context.Contratos.AnyAsync(c => c.Id == model.ContratoId))
        {
            ModelState.AddModelError(nameof(model.ContratoId), "Selecione um contrato.");
        }

        if (model.FuncaoId == Guid.Empty || !await _context.Funcoes.AnyAsync(f => f.Id == model.FuncaoId))
        {
            ModelState.AddModelError(nameof(model.FuncaoId), "Selecione uma função.");
        }

        if (model.AdicionalId == Guid.Empty || !await _context.TiposAdicionais.AnyAsync(a => a.Id == model.AdicionalId))
        {
            ModelState.AddModelError(nameof(model.AdicionalId), "Selecione um tipo de adicional.");
        }
    }

    private void RemoverErrosDeNavegacao()
    {
        ModelState.Remove(nameof(ContratoFuncaoAdicional.Contrato));
        ModelState.Remove(nameof(ContratoFuncaoAdicional.Funcao));
        ModelState.Remove(nameof(ContratoFuncaoAdicional.Adicional));
    }

    private async Task PopularCombosAsync()
    {
        ViewBag.Contratos = await _context.Contratos
            .OrderBy(c => c.EmpresaNome)
            .ToListAsync();
        ViewBag.Funcoes = await _context.Funcoes
            .OrderBy(f => f.Nome)
            .ToListAsync();
        ViewBag.Adicionais = await _context.TiposAdicionais
            .OrderBy(a => a.Nome)
            .ToListAsync();
    }
}
