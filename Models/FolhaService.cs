using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cooperativa.Data;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Models
{
    public class FolhaService
{
    private readonly CooperativaDbContext _context;

    public FolhaService(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<FolhaPagamento> GerarFolhaAsync(Guid cooperadoId, int ano, int mes, decimal valorHora)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var registros = await _context.RegistrosPonto
            .Where(r => r.CooperadoId == cooperadoId && r.Data >= inicio && r.Data <= fim)
            .ToListAsync();

        var horasTotal = registros.Sum(r =>
        {
            if (r.Entrada is null || r.Saida is null)
            {
                return 0m;
            }

            var diff = r.Saida.Value - r.Entrada.Value;
            return (decimal)diff.TotalHours;
        });

        var salarioBruto = (decimal)horasTotal * valorHora;
        var descontos = salarioBruto * 0.08m;

        var folha = new FolhaPagamento
        {
            Id = Guid.NewGuid(),
            CooperadoId = cooperadoId,
            Competencia = new DateTime(ano, mes, 1),
            Salario = salarioBruto,
            Adicional = 0m,
            Descontos = descontos
        };

        _context.FolhasPagamento.Add(folha);
        await _context.SaveChangesAsync();

        return folha;
    }
}

}