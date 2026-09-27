using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

/// <summary>Configuração única da cooperativa: sempre há no máximo um registro.</summary>
public class ConfiguracaoHoraNoturnaController : Controller
{
    private readonly CooperativaDbContext _context;

    public ConfiguracaoHoraNoturnaController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var configuracao = await ObterOuCriarAsync();
        return View(configuracao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConfiguracaoHoraNoturna model)
    {
        ModelState.Remove(nameof(ConfiguracaoHoraNoturna.Id));

        if (model.HoraInicio == model.HoraFim)
        {
            ModelState.AddModelError(nameof(model.HoraFim), "O horário de fim deve ser diferente do início.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var configuracao = await ObterOuCriarAsync();
        configuracao.HoraInicio = model.HoraInicio;
        configuracao.HoraFim = model.HoraFim;
        configuracao.HoraReduzida = model.HoraReduzida;
        await _context.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Configuração de hora noturna atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ConfiguracaoHoraNoturna> ObterOuCriarAsync()
    {
        var configuracao = await _context.ConfiguracoesHoraNoturna.FirstOrDefaultAsync();
        if (configuracao == null)
        {
            configuracao = new ConfiguracaoHoraNoturna { Id = Guid.NewGuid() };
            _context.ConfiguracoesHoraNoturna.Add(configuracao);
            await _context.SaveChangesAsync();
        }

        return configuracao;
    }
}
