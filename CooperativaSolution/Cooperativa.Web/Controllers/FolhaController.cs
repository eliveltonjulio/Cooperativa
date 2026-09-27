using System.Text.Json;
using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Helpers;
using Cooperativa.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class FolhaController : Controller
{
    private readonly CooperativaDbContext _context;

    public FolhaController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca, DateTime? competencia)
    {
        var folhas = _context.FolhasPagamento
            .Include(f => f.Cooperado)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            folhas = folhas.Where(f => f.Cooperado!.Nome.Contains(termo));
        }

        if (competencia.HasValue)
        {
            folhas = folhas.Where(f => f.Competencia.Year == competencia.Value.Year && f.Competencia.Month == competencia.Value.Month);
        }

        var lista = await folhas
            .OrderByDescending(f => f.Competencia)
            .ToListAsync();

        ViewBag.Busca = busca;
        ViewBag.Competencia = competencia?.ToString("yyyy-MM");
        return View(lista);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FolhaPagamento model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Competencia = DateTimeHelper.NormalizeToUtc(model.Competencia == default ? DateTime.UtcNow : model.Competencia, DateTime.UtcNow);

        _context.FolhasPagamento.Add(model);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Folha de pagamento cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formulário de geração automática de folha por competência e escopo (cooperado ou contrato).</summary>
    [HttpGet]
    public async Task<IActionResult> Gerar()
    {
        await PopularCombosGerarAsync();
        var hoje = DateTime.UtcNow;
        var model = new GerarFolhaViewModel
        {
            Escopo = GerarFolhaViewModel.EscopoCooperado,
            Competencia = new DateTime(hoje.Year, hoje.Month, 1)
        };
        return View(model);
    }

    /// <summary>Executa o cálculo da folha com base em ponto, remunerações, adicionais e despesas cadastrados.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gerar(GerarFolhaViewModel model)
    {
        if (model.Competencia is null || model.Competencia.Value == default)
        {
            ModelState.AddModelError(nameof(model.Competencia), "Informe o mês/ano da competência.");
        }

        if (string.Equals(model.Escopo, GerarFolhaViewModel.EscopoCooperado, StringComparison.OrdinalIgnoreCase))
        {
            if (model.CooperadoId is null || model.CooperadoId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.CooperadoId), "Selecione um cooperado.");
            }
        }
        else if (string.Equals(model.Escopo, GerarFolhaViewModel.EscopoContrato, StringComparison.OrdinalIgnoreCase))
        {
            if (model.ContratoId is null || model.ContratoId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.ContratoId), "Selecione um contrato.");
            }
        }
        else
        {
            ModelState.AddModelError(nameof(model.Escopo), "Escopo de geração inválido.");
        }

        if (!ModelState.IsValid)
        {
            await PopularCombosGerarAsync();
            return View(model);
        }

        var escopoCooperado = string.Equals(model.Escopo, GerarFolhaViewModel.EscopoCooperado, StringComparison.OrdinalIgnoreCase);
        var competencia = new DateTime(model.Competencia!.Value.Year, model.Competencia.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimCompetencia = competencia.AddMonths(1);
        var geradas = 0;
        var ignoradas = 0;

        var configuracaoNoturna = await _context.ConfiguracoesHoraNoturna.FirstOrDefaultAsync();
        var feriados = await _context.Feriados
            .Where(f => f.Data >= competencia && f.Data < fimCompetencia)
            .ToListAsync();
        var despesas = await _context.DespesasFolha.Where(d => d.Ativo).ToListAsync();
        var regrasHoraExtra = await _context.RegrasHoraExtra.ToListAsync();

        var cooperadosAlvo = escopoCooperado
            ? new List<Guid> { model.CooperadoId!.Value }
            : await _context.Alocacoes
                .Where(a => a.ContratoId == model.ContratoId
                    && a.DataInicio < fimCompetencia
                    && (a.DataFim == null || a.DataFim >= competencia))
                .Select(a => a.CooperadoId)
                .Distinct()
                .ToListAsync();

        foreach (var cooperadoId in cooperadosAlvo)
        {
            var alocacoes = await _context.Alocacoes
                .Include(a => a.Contrato)
                .Where(a => a.CooperadoId == cooperadoId
                    && a.DataInicio < fimCompetencia
                    && (a.DataFim == null || a.DataFim >= competencia))
                .ToListAsync();

            if (!escopoCooperado)
            {
                alocacoes = alocacoes.Where(a => a.ContratoId == model.ContratoId).ToList();
            }

            if (alocacoes.Count == 0)
            {
                ignoradas++;
                continue;
            }

            var registros = await _context.RegistrosPonto
                .Where(r => r.CooperadoId == cooperadoId && r.Data >= competencia && r.Data < fimCompetencia)
                .ToListAsync();

            var funcoesIds = alocacoes.Select(a => a.FuncaoId).Distinct().ToList();
            var contratosIds = alocacoes.Select(a => a.ContratoId).Distinct().ToList();

            var jornadas = await _context.JornadasContratuais
                .Where(j => contratosIds.Contains(j.ContratoId) && funcoesIds.Contains(j.FuncaoId))
                .ToListAsync();

            var remuneracoes = await _context.Remuneracoes
                .Where(r => contratosIds.Contains(r.ContratoId)
                    && funcoesIds.Contains(r.FuncaoId)
                    && r.DataInicio < fimCompetencia
                    && (r.DataFim == null || r.DataFim >= competencia))
                .ToListAsync();

            var adicionais = await _context.ContratosFuncoesAdicionais
                .Include(c => c.Adicional)
                .Where(c => contratosIds.Contains(c.ContratoId) && funcoesIds.Contains(c.FuncaoId))
                .ToListAsync();

            var resultado = CalculadoraFolhaPagamento.Calcular(
                competencia,
                fimCompetencia,
                registros,
                alocacoes,
                jornadas,
                remuneracoes,
                adicionais,
                regrasHoraExtra,
                despesas,
                configuracaoNoturna,
                feriados);

            if (resultado == null || (resultado.TotalBruto == 0 && resultado.TotalDescontos == 0))
            {
                ignoradas++;
                continue;
            }

            // Regeneração: substitui folhas já existentes do cooperado na competência.
            var existentes = await _context.FolhasPagamento
                .Where(f => f.CooperadoId == cooperadoId
                    && f.Competencia.Year == competencia.Year
                    && f.Competencia.Month == competencia.Month)
                .ToListAsync();
            _context.FolhasPagamento.RemoveRange(existentes);

            _context.FolhasPagamento.Add(new FolhaPagamento
            {
                Id = Guid.NewGuid(),
                CooperadoId = cooperadoId,
                Competencia = competencia,
                Salario = resultado.ValorRemuneracaoBase,
                Adicional = resultado.ValorHorasExtras + resultado.ValorAdicionalNoturno + resultado.ValorAdicionais,
                Descontos = resultado.TotalDescontos,
                Detalhes = JsonSerializer.Serialize(resultado.Detalhe)
            });
            geradas++;
        }

        await _context.SaveChangesAsync();

        if (geradas > 0)
        {
            TempData["MensagemSucesso"] = geradas == 1
                ? $"1 folha gerada para {competencia:MM/yyyy}."
                : $"{geradas} folhas geradas para {competencia:MM/yyyy}.";
        }
        else
        {
            TempData["MensagemErro"] = $"Nenhuma folha foi gerada para {competencia:MM/yyyy}.";
        }

        if (ignoradas > 0)
        {
            TempData["MensagemErro"] = $"{ignoradas} cooperado(s) foram ignorados por falta de dados (alocação, registros de ponto ou remuneração no período).";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCombosGerarAsync()
    {
        ViewBag.Cooperados = await _context.Cooperados
            .Where(c => c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync();
        ViewBag.Contratos = await _context.Contratos
            .OrderBy(c => c.EmpresaNome)
            .ToListAsync();
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var folha = await _context.FolhasPagamento
            .Include(f => f.Cooperado)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (folha == null)
        {
            return NotFound();
        }

        // Detalhamento persistido no momento da geração (nulo em folhas manuais).
        if (!string.IsNullOrWhiteSpace(folha.Detalhes))
        {
            try
            {
                var detalhe = JsonSerializer.Deserialize<DetalheFolha>(folha.Detalhes);
                if (detalhe != null && detalhe.Grupos.Count > 0)
                {
                    var idsContratos = detalhe.Grupos.Select(g => g.ContratoId).Distinct().ToList();
                    var idsFuncoes = detalhe.Grupos.Select(g => g.FuncaoId).Distinct().ToList();

                    ViewBag.Detalhe = detalhe;
                    ViewBag.NomesContratos = await _context.Contratos
                        .Where(c => idsContratos.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id, c => c.EmpresaNome);
                    ViewBag.NomesFuncoes = await _context.Funcoes
                        .Where(f => idsFuncoes.Contains(f.Id))
                        .ToDictionaryAsync(f => f.Id, f => f.Nome);
                }
            }
            catch (JsonException)
            {
                ViewBag.Detalhe = null;
            }
        }

        return View(folha);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var folha = await _context.FolhasPagamento.FirstOrDefaultAsync(f => f.Id == id);
        if (folha == null)
        {
            return NotFound();
        }

        ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
        return View(folha);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FolhaPagamento model)
    {
        var folha = await _context.FolhasPagamento.FirstOrDefaultAsync(f => f.Id == id);
        if (folha == null)
        {
            return NotFound();
        }

        if (model.CooperadoId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Selecione um cooperado.");
            ViewBag.Cooperados = await _context.Cooperados.OrderBy(c => c.Nome).ToListAsync();
            return View(model);
        }

        folha.CooperadoId = model.CooperadoId;
        folha.Competencia = DateTimeHelper.NormalizeToUtc(model.Competencia == default ? folha.Competencia : model.Competencia, folha.Competencia);
        folha.Salario = model.Salario;
        folha.Adicional = model.Adicional;
        folha.Descontos = model.Descontos;
        // Ajuste manual invalida o detalhamento gerado automaticamente.
        folha.Detalhes = null;

        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Folha de pagamento atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var folha = await _context.FolhasPagamento
            .Include(f => f.Cooperado)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (folha == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Folha",
            DisplayName = $"Folha de {folha.Cooperado?.Nome} - {folha.Competencia:MM/yyyy}",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var folha = await _context.FolhasPagamento.FirstOrDefaultAsync(f => f.Id == id);
        if (folha == null)
        {
            return NotFound();
        }

        _context.FolhasPagamento.Remove(folha);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Folha de pagamento excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
