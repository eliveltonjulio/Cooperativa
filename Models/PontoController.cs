using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cooperativa.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Models
{
    [Authorize]
public class PontoController : Controller
{
    private readonly CooperativaDbContext _context;

    public PontoController(CooperativaDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(Guid cooperadoId, DateTime data, TimeSpan entrada, TimeSpan saida)
    {
        var dataPonto = data.Date;
        var cooperadoAlocado = await _context.Alocacoes
            .AnyAsync(a => a.CooperadoId == cooperadoId
                && a.DataInicio <= dataPonto
                && (a.DataFim == null || a.DataFim >= dataPonto));

        if (!cooperadoAlocado)
        {
            TempData["MensagemErro"] = "Cooperado não alocado";
            return RedirectToAction("Detalhes", "Cooperados", new { id = cooperadoId });
        }

        if (saida < entrada)
        {
            TempData["MensagemErro"] = "A hora de saída não pode ser anterior à hora de entrada.";
            return RedirectToAction("Detalhes", "Cooperados", new { id = cooperadoId });
        }

        var registro = new RegistroPonto
        {
            Id = Guid.NewGuid(),
            CooperadoId = cooperadoId,
            Data = dataPonto,
            Entrada = dataPonto.Add(entrada),
            Saida = dataPonto.Add(saida),
            Observacao = string.Empty
        };

        _context.RegistrosPonto.Add(registro);
        await _context.SaveChangesAsync();

        return RedirectToAction("Detalhes", "Cooperados", new { id = cooperadoId });
    }
}

}