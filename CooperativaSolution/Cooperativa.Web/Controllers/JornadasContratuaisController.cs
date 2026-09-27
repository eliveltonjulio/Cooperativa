using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class JornadasContratuaisController : Controller
{
    private readonly CooperativaDbContext _context;

    public JornadasContratuaisController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var jornadas = _context.JornadasContratuais
            .Include(j => j.Contrato)
            .Include(j => j.Funcao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            jornadas = jornadas.Where(j =>
                (j.Contrato != null && EF.Functions.ILike(j.Contrato.EmpresaNome, $"%{termo}%")) ||
                (j.Funcao != null && EF.Functions.ILike(j.Funcao.Nome, $"%{termo}%")));
        }

        ViewBag.Busca = busca;

        var listaOrdenada = await jornadas.OrderBy(j => j.Contrato!.EmpresaNome)
                                          .ThenBy(j => j.Funcao!.Nome)
                                          .ThenBy(j => j.HoraInicio)
                                          .ToListAsync();

        return View(listaOrdenada);
    }

    public async Task<IActionResult> Create()
    {
        await PopularCombosAsync();
        return View(JornadaSemanalViewModel.CriarPadrao());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JornadaSemanalViewModel model)
    {
        await ValidarJornadaSemanalAsync(model, contratoIdOriginal: null, funcaoIdOriginal: null);

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        var novasJornadas = AgruparDiasEmJornadas(model);
        _context.JornadasContratuais.AddRange(novasJornadas);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Jornada contratual cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var jornada = await _context.JornadasContratuais
            .Include(j => j.Contrato)
            .Include(j => j.Funcao)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (jornada == null) return NotFound();

        var todasDaMesmaFuncao = await _context.JornadasContratuais
            .Where(j => j.ContratoId == jornada.ContratoId && j.FuncaoId == jornada.FuncaoId)
            .OrderBy(j => j.HoraInicio)
            .ToListAsync();

        ViewBag.OutrasJornadas = todasDaMesmaFuncao;
        return View(jornada);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var jornada = await _context.JornadasContratuais
            .Include(j => j.Contrato)
            .Include(j => j.Funcao)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (jornada == null) return NotFound();

        var todas = await _context.JornadasContratuais
            .Where(j => j.ContratoId == jornada.ContratoId && j.FuncaoId == jornada.FuncaoId)
            .ToListAsync();

        var viewModel = new JornadaSemanalViewModel
        {
            IdOriginal = id,
            ContratoId = jornada.ContratoId,
            FuncaoId = jornada.FuncaoId,
            Dias = new List<DiaJornadaInputModel>()
        };

        var diasPadrao = JornadaSemanalViewModel.CriarPadrao().Dias;
        foreach (var dia in diasPadrao)
        {
            var jornadaDoDia = todas.FirstOrDefault(j => (j.DiasSemana & dia.DiaSemanaValor) != 0);
            if (jornadaDoDia != null)
            {
                viewModel.Dias.Add(new DiaJornadaInputModel
                {
                    DiaSemanaValor = dia.DiaSemanaValor,
                    NomeDia = dia.NomeDia,
                    Ativo = true,
                    HoraInicio = jornadaDoDia.HoraInicio,
                    HoraFim = jornadaDoDia.HoraFim,
                    HoraInicioIntervalo = jornadaDoDia.HoraInicioIntervalo,
                    HoraFimIntervalo = jornadaDoDia.HoraFimIntervalo
                });
            }
            else
            {
                viewModel.Dias.Add(new DiaJornadaInputModel
                {
                    DiaSemanaValor = dia.DiaSemanaValor,
                    NomeDia = dia.NomeDia,
                    Ativo = false,
                    HoraInicio = dia.HoraInicio,
                    HoraFim = dia.HoraFim,
                    HoraInicioIntervalo = null,
                    HoraFimIntervalo = null
                });
            }
        }

        await PopularCombosAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, JornadaSemanalViewModel model)
    {
        var jornadaOriginal = await _context.JornadasContratuais.FindAsync(id);
        if (jornadaOriginal == null) return NotFound();

        await ValidarJornadaSemanalAsync(model, jornadaOriginal.ContratoId, jornadaOriginal.FuncaoId);

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync();
            return View(model);
        }

        var jornadasAntigas = await _context.JornadasContratuais
            .Where(j => j.ContratoId == jornadaOriginal.ContratoId && j.FuncaoId == jornadaOriginal.FuncaoId)
            .ToListAsync();

        _context.JornadasContratuais.RemoveRange(jornadasAntigas);

        var novasJornadas = AgruparDiasEmJornadas(model);
        _context.JornadasContratuais.AddRange(novasJornadas);
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Jornada contratual atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var jornada = await _context.JornadasContratuais
            .Include(j => j.Contrato)
            .Include(j => j.Funcao)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (jornada == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "JornadasContratuais",
            ActionName = "Delete",
            DisplayName = $"Jornada para {jornada.Funcao?.Nome} no contrato {jornada.Contrato?.EmpresaNome} ({jornada.HoraInicio:hh\\:mm} às {jornada.HoraFim:hh\\:mm})",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var jornada = await _context.JornadasContratuais.FindAsync(id);
        if (jornada == null) return NotFound();

        _context.JornadasContratuais.Remove(jornada);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Jornada contratual excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarJornadaSemanalAsync(JornadaSemanalViewModel model, Guid? contratoIdOriginal, Guid? funcaoIdOriginal)
    {
        if (model.ContratoId == Guid.Empty || !await _context.Contratos.AnyAsync(c => c.Id == model.ContratoId))
        {
            ModelState.AddModelError(nameof(model.ContratoId), "Selecione um contrato.");
        }

        if (model.FuncaoId == Guid.Empty || !await _context.Funcoes.AnyAsync(f => f.Id == model.FuncaoId))
        {
            ModelState.AddModelError(nameof(model.FuncaoId), "Selecione uma função.");
        }

        var diasAtivos = model.Dias?.Where(d => d.Ativo).ToList() ?? new List<DiaJornadaInputModel>();
        if (diasAtivos.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione ao menos um dia da semana para a jornada.");
            return;
        }

        for (int i = 0; i < (model.Dias?.Count ?? 0); i++)
        {
            var dia = model.Dias![i];
            if (!dia.Ativo) continue;

            if (dia.HoraFim == dia.HoraInicio)
            {
                ModelState.AddModelError($"Dias[{i}].HoraFim", $"{dia.NomeDia}: o horário de início e fim da jornada não podem ser iguais.");
            }

            if (dia.HoraInicioIntervalo.HasValue != dia.HoraFimIntervalo.HasValue)
            {
                ModelState.AddModelError($"Dias[{i}].HoraInicioIntervalo", $"{dia.NomeDia}: informe o início e o fim do intervalo.");
            }
            else if (dia.HoraInicioIntervalo.HasValue && dia.HoraFimIntervalo.HasValue)
            {
                var baseData = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var iniJornada = baseData.Add(dia.HoraInicio);
                var fimJornada = dia.HoraFim > dia.HoraInicio ? baseData.Add(dia.HoraFim) : baseData.AddDays(1).Add(dia.HoraFim);

                var iniIntervalo = dia.HoraInicioIntervalo.Value >= dia.HoraInicio
                    ? baseData.Add(dia.HoraInicioIntervalo.Value)
                    : baseData.AddDays(1).Add(dia.HoraInicioIntervalo.Value);

                var fimIntervalo = dia.HoraFimIntervalo.Value > dia.HoraInicioIntervalo.Value
                    ? iniIntervalo.Date.Add(dia.HoraFimIntervalo.Value)
                    : (dia.HoraFimIntervalo.Value >= dia.HoraInicio
                        ? baseData.Add(dia.HoraFimIntervalo.Value)
                        : baseData.AddDays(1).Add(dia.HoraFimIntervalo.Value));

                if (fimIntervalo <= iniIntervalo || iniIntervalo < iniJornada || fimIntervalo > fimJornada)
                {
                    ModelState.AddModelError($"Dias[{i}].HoraInicioIntervalo", $"{dia.NomeDia}: o intervalo deve estar contido entre o início e o fim da jornada.");
                }
            }
        }

        var mudouEmpresaOuFuncao = contratoIdOriginal == null
            || model.ContratoId != contratoIdOriginal
            || model.FuncaoId != funcaoIdOriginal;

        if (mudouEmpresaOuFuncao && model.ContratoId != Guid.Empty && model.FuncaoId != Guid.Empty)
        {
            var bitmaskTotal = diasAtivos.Aggregate(0, (acc, d) => acc | d.DiaSemanaValor);
            var jornadasExistentes = await _context.JornadasContratuais
                .Where(j => j.ContratoId == model.ContratoId && j.FuncaoId == model.FuncaoId)
                .ToListAsync();

            var diasConflitantes = jornadasExistentes
                .Where(j => (j.DiasSemana & bitmaskTotal) != 0)
                .SelectMany(j => DiaSemanaFlags.Todos.Where(d => (d.Valor & j.DiasSemana & bitmaskTotal) != 0).Select(d => d.Nome))
                .Distinct()
                .ToList();

            if (diasConflitantes.Count != 0)
            {
                ModelState.AddModelError(string.Empty, $"Já existe jornada cadastrada para este contrato e função nos dias: {string.Join(", ", diasConflitantes)}.");
            }
        }
    }

    private static List<JornadaContratual> AgruparDiasEmJornadas(JornadaSemanalViewModel model)
    {
        var diasAtivos = model.Dias.Where(d => d.Ativo).ToList();

        var grupos = diasAtivos.GroupBy(d => new
        {
            d.HoraInicio,
            d.HoraFim,
            d.HoraInicioIntervalo,
            d.HoraFimIntervalo
        });

        var resultado = new List<JornadaContratual>();
        foreach (var g in grupos)
        {
            var bitmask = g.Aggregate(0, (acc, d) => acc | d.DiaSemanaValor);
            resultado.Add(new JornadaContratual
            {
                Id = Guid.NewGuid(),
                ContratoId = model.ContratoId,
                FuncaoId = model.FuncaoId,
                HoraInicio = g.Key.HoraInicio,
                HoraFim = g.Key.HoraFim,
                HoraInicioIntervalo = g.Key.HoraInicioIntervalo,
                HoraFimIntervalo = g.Key.HoraFimIntervalo,
                DiasSemana = bitmask
            });
        }

        return resultado;
    }

    private async Task PopularCombosAsync()
    {
        ViewBag.Contratos = await _context.Contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
        ViewBag.Funcoes = await _context.Funcoes.OrderBy(f => f.Nome).ToListAsync();
        ViewBag.DiasSemana = DiaSemanaFlags.Todos;
    }
}
