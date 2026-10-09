using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using System.Text.Json;

namespace Cooperativa.Web.Controllers
{
    [Authorize]
    public class PontoController : Controller
    {
    private const string MensagemPontoDuplicado = "Já existe um registro de ponto para este cooperado nesta data.";

    private readonly CooperativaDbContext _context;

    public PontoController(CooperativaDbContext context)
    {
        _context = context;
    }

    // Ação Index para responder a GET /Ponto
    public async Task<IActionResult> Index(string busca, Guid? contratoId, DateTime? data, string competencia, string ocorrencia)
    {
        var registros = _context.RegistrosPonto
            .Include(r => r.Cooperado)
            .AsQueryable();

        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            var cooperadoIdStr = User.FindFirst("CooperadoId")?.Value;
            if (Guid.TryParse(cooperadoIdStr, out var meuCooperadoId))
            {
                registros = registros.Where(r => r.CooperadoId == meuCooperadoId);
            }
        }

        var ehCoordenador = User.IsInRole("Coordenador") || User.IsInRole("Gestor");
        var equipeCoordenador = User.FindFirst("Equipe")?.Value;
        if (ehCoordenador && !string.IsNullOrWhiteSpace(equipeCoordenador))
        {
            registros = registros.Where(r => r.Cooperado != null && (r.Cooperado.Equipe == equipeCoordenador || r.Cooperado.Equipe == null || r.Cooperado.Equipe == ""));
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            registros = registros.Where(r =>
                (r.Cooperado != null && EF.Functions.ILike(r.Cooperado.Nome, $"%{termo}%"))
                || (!string.IsNullOrEmpty(r.Observacao) && EF.Functions.ILike(r.Observacao, $"%{termo}%")));
        }

        // Filtro por contrato: cooperado com alocação no contrato cobrindo a data do registro.
        if (contratoId.HasValue && contratoId.Value != Guid.Empty)
        {
            var contratoSelecionado = contratoId.Value;
            registros = registros.Where(r => _context.Alocacoes.Any(a =>
                a.CooperadoId == r.CooperadoId
                && a.ContratoId == contratoSelecionado
                && a.DataInicio.Date <= r.Data.Date
                && (a.DataFim == null || a.DataFim >= r.Data.Date)));
        }

        // Filtro por data exata do registro.
        // A coluna é "timestamp with time zone": o parâmetro precisa ter Kind=Utc.
        if (data.HasValue)
        {
            var dia = DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Utc);
            registros = registros.Where(r => r.Data >= dia && r.Data < dia.AddDays(1));
        }

        // Filtro por competência (mês/ano, formato yyyy-MM).
        // A coluna é "timestamp with time zone": os limites precisam ter Kind=Utc.
        if (!string.IsNullOrWhiteSpace(competencia))
        {
            if (DateTime.TryParseExact(
                    competencia.Trim() + "-01",
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var primeiroDiaMes))
            {
                var inicioDoMes = DateTime.SpecifyKind(primeiroDiaMes, DateTimeKind.Utc);
                var fimDoMes = inicioDoMes.AddMonths(1);
                registros = registros.Where(r => r.Data >= inicioDoMes && r.Data < fimDoMes);
            }
            else
            {
                TempData["MensagemErro"] = "Informe uma competência válida (mês/ano).";
            }
        }

        // Filtro por ocorrência registrada no ponto (catálogo OcorrenciasPonto).
        if (!string.IsNullOrWhiteSpace(ocorrencia))
        {
            registros = registros.Where(r => r.Ocorrencia == ocorrencia.Trim());
        }

        var lista = await registros.OrderByDescending(r => r.Data).ToListAsync();

        ViewBag.Busca = busca;
        ViewBag.Data = data?.ToString("yyyy-MM-dd");
        ViewBag.ContratoId = contratoId;
        ViewBag.Competencia = competencia;
        ViewBag.Ocorrencia = ocorrencia;
        ViewBag.Contratos = await _context.Contratos
            .OrderBy(c => c.EmpresaNome)
            .ToListAsync();

        return View(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(Guid cooperadoId, DateTime data, TimeSpan entrada, TimeSpan saida, TimeSpan? inicioIntervalo, TimeSpan? fimIntervalo, string? ocorrencia)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem registrar ponto.";
            return RedirectToAction(nameof(Index));
        }

        // Normaliza a data para UTC para consistência com o resto da aplicação
        var dataPonto = new DateTime(data.Year, data.Month, data.Day, 0, 0, 0, DateTimeKind.Utc);

        var pontoJaRegistrado = await _context.RegistrosPonto.AnyAsync(r =>
            r.CooperadoId == cooperadoId
            && r.Data >= dataPonto
            && r.Data < dataPonto.AddDays(1));

        if (pontoJaRegistrado)
        {
            TempData["MensagemErro"] = MensagemPontoDuplicado;
            return RedirectToAction("Details", "Cooperados", new { id = cooperadoId });
        }

        var cooperadoAlocado = await _context.Alocacoes
            .AnyAsync(a => a.CooperadoId == cooperadoId
                && a.DataInicio.Date <= dataPonto
                && (a.DataFim == null || a.DataFim >= dataPonto));

        if (!cooperadoAlocado)
        {
            TempData["MensagemErro"] = "Cooperado não alocado";
            return RedirectToAction("Details", "Cooperados", new { id = cooperadoId });
        }

        var registro = new RegistroPonto
        {
            Id = Guid.NewGuid(),
            CooperadoId = cooperadoId,
            Data = dataPonto,
            Entrada = dataPonto.Add(entrada),
            Saida = dataPonto.Add(saida < entrada ? saida.Add(TimeSpan.FromDays(1)) : saida),
            InicioIntervalo = inicioIntervalo.HasValue ? dataPonto.Add(inicioIntervalo.Value < entrada ? inicioIntervalo.Value.Add(TimeSpan.FromDays(1)) : inicioIntervalo.Value) : null,
            FimIntervalo = fimIntervalo.HasValue ? dataPonto.Add(fimIntervalo.Value < entrada ? fimIntervalo.Value.Add(TimeSpan.FromDays(1)) : fimIntervalo.Value) : null,
            Ocorrencia = OcorrenciasPonto.ObterPorCodigo(ocorrencia)?.Codigo,
            Observacao = string.Empty
        };

        try
        {
            _context.RegistrosPonto.Add(registro);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhViolacaoDeUnicidade(ex))
        {
            // Corrida entre requisições: o índice único do banco impede a duplicata
            // mesmo que a validação da aplicação tenha passado.
            TempData["MensagemErro"] = MensagemPontoDuplicado;
            return RedirectToAction("Details", "Cooperados", new { id = cooperadoId });
        }

        TempData["MensagemSucesso"] = "Ponto registrado com sucesso.";
        return RedirectToAction("Details", "Cooperados", new { id = cooperadoId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var registro = await _context.RegistrosPonto
            .Include(r => r.Cooperado)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (registro == null) return NotFound();

        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            var cooperadoIdStr = User.FindFirst("CooperadoId")?.Value;
            if (Guid.TryParse(cooperadoIdStr, out var meuCooperadoId) && registro.CooperadoId != meuCooperadoId)
            {
                TempData["MensagemErro"] = "Você só pode consultar o seu próprio registro de ponto.";
                return RedirectToAction(nameof(Index));
            }
        }

        return View(registro);
    }

    public async Task<IActionResult> Create()
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem cadastrar registros de ponto.";
            return RedirectToAction(nameof(Index));
        }

        await CarregarContratosEAlocacoesAsync();
        return View(new RegistroPonto { Data = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid contratoId, RegistroPonto model)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem cadastrar registros de ponto.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(RegistroPonto.Cooperado));
        await ValidarRegistroAsync(model, contratoId);

        if (!ModelState.IsValid)
        {
            ViewData["ContratoId"] = contratoId;
            await CarregarContratosEAlocacoesAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Observacao ??= string.Empty;
        // Normaliza a ocorrência para o código canônico do catálogo (nulo quando vazia/inválida).
        model.Ocorrencia = OcorrenciasPonto.ObterPorCodigo(model.Ocorrencia)?.Codigo;
        NormalizarDatas(model);
        try
        {
            _context.RegistrosPonto.Add(model);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhViolacaoDeUnicidade(ex))
        {
            // Corrida entre requisições: o índice único do banco impede a duplicata
            // mesmo que a validação da aplicação tenha passado.
            _context.Entry(model).State = EntityState.Detached;
            ModelState.AddModelError(nameof(model.Data), MensagemPontoDuplicado);
            ViewData["ContratoId"] = contratoId;
            await CarregarContratosEAlocacoesAsync();
            return View(model);
        }

        TempData["MensagemSucesso"] = "Ponto registrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem alterar o ponto, inclusive o seu próprio.";
            return RedirectToAction(nameof(Index));
        }

        var registro = await _context.RegistrosPonto.FirstOrDefaultAsync(r => r.Id == id);
        if (registro == null) return NotFound();

        await PopularCombosAsync(incluirInativos: true);
        return View(registro);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RegistroPonto model)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem alterar o ponto, inclusive o seu próprio.";
            return RedirectToAction(nameof(Index));
        }

        var registro = await _context.RegistrosPonto.FirstOrDefaultAsync(r => r.Id == id);
        if (registro == null) return NotFound();

        ModelState.Remove(nameof(RegistroPonto.Cooperado));
        await ValidarRegistroAsync(model, exigirCooperadoAtivo: false, registroIdIgnorado: id);

        if (!ModelState.IsValid)
        {
            await PopularCombosAsync(incluirInativos: true);
            return View(model);
        }

        registro.CooperadoId = model.CooperadoId;
        registro.Observacao = model.Observacao ?? string.Empty;
        // Normaliza a ocorrência para o código canônico do catálogo (nulo quando vazia/inválida).
        registro.Ocorrencia = OcorrenciasPonto.ObterPorCodigo(model.Ocorrencia)?.Codigo;
        NormalizarDatas(model);
        registro.Data = model.Data;
        registro.Entrada = model.Entrada;
        registro.Saida = model.Saida;
        registro.InicioIntervalo = model.InicioIntervalo;
        registro.FimIntervalo = model.FimIntervalo;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhViolacaoDeUnicidade(ex))
        {
            // Corrida entre requisições: o índice único do banco impede a duplicata
            // mesmo que a validação da aplicação tenha passado.
            ModelState.AddModelError(nameof(model.Data), MensagemPontoDuplicado);
            await PopularCombosAsync(incluirInativos: true);
            return View(model);
        }

        TempData["MensagemSucesso"] = "Registro de ponto atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem excluir o ponto, inclusive o seu próprio.";
            return RedirectToAction(nameof(Index));
        }

        var registro = await _context.RegistrosPonto
            .Include(r => r.Cooperado)
            .FirstOrDefaultAsync(r => r.Id == id);
        return registro == null ? NotFound() : View(registro);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem excluir o ponto, inclusive o seu próprio.";
            return RedirectToAction(nameof(Index));
        }

        var registro = await _context.RegistrosPonto.FirstOrDefaultAsync(r => r.Id == id);
        if (registro == null) return NotFound();

        _context.RegistrosPonto.Remove(registro);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Registro de ponto excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCombosAsync(bool incluirInativos = false)
    {
        var cooperados = _context.Cooperados.AsQueryable();
        if (!incluirInativos)
        {
            cooperados = cooperados.Where(cooperado => cooperado.Ativo);
        }

        ViewBag.Cooperados = await cooperados.OrderBy(cooperado => cooperado.Nome).ToListAsync();
    }

    private async Task CarregarContratosEAlocacoesAsync()
    {
        ViewBag.Contratos = await _context.Contratos
            .OrderBy(c => c.EmpresaNome)
            .ToListAsync();

        var alocacoes = await _context.Alocacoes
            .Where(a => a.Cooperado != null && a.Cooperado.Ativo)
            .Select(a => new
            {
                ContratoId = a.ContratoId,
                CooperadoId = a.CooperadoId,
                Nome = a.Cooperado!.Nome,
                Inicio = a.DataInicio,
                Fim = a.DataFim
            })
            .ToListAsync();

        var dados = alocacoes.Select(a => new
        {
            contratoId = a.ContratoId.ToString(),
            cooperadoId = a.CooperadoId.ToString(),
            nome = a.Nome,
            inicio = a.Inicio.ToString("yyyy-MM-dd"),
            fim = a.Fim.HasValue ? a.Fim.Value.ToString("yyyy-MM-dd") : null
        });

        ViewBag.AlocacoesJson = JsonSerializer.Serialize(dados);
    }

    private async Task ValidarRegistroAsync(RegistroPonto model, Guid? contratoId = null, bool exigirCooperadoAtivo = true, Guid? registroIdIgnorado = null)
    {
        if (contratoId.HasValue && contratoId.Value == Guid.Empty)
        {
            ModelState.AddModelError("contratoId", "Selecione um contrato.");
        }

        if (model.CooperadoId == Guid.Empty || !await _context.Cooperados.AnyAsync(c =>
                c.Id == model.CooperadoId && (!exigirCooperadoAtivo || c.Ativo)))
        {
            ModelState.AddModelError(nameof(model.CooperadoId), exigirCooperadoAtivo
                ? "Selecione um cooperado ativo."
                : "Selecione um cooperado.");
        }

        if (model.Data == default)
        {
            ModelState.AddModelError(nameof(model.Data), "Informe a data do ponto.");
        }

        if (!string.IsNullOrWhiteSpace(model.Ocorrencia) && !OcorrenciasPonto.Existe(model.Ocorrencia))
        {
            ModelState.AddModelError(nameof(model.Ocorrencia), "Selecione uma ocorrência válida.");
        }

        if (model.InicioIntervalo.HasValue != model.FimIntervalo.HasValue)
        {
            ModelState.AddModelError(nameof(model.InicioIntervalo), "Informe o início e o fim do intervalo.");
        }
        else if (model.InicioIntervalo.HasValue && model.Entrada.HasValue && model.Saida.HasValue)
        {
            var dataPonto = model.Data.Date;
            var entrada = dataPonto.Add(model.Entrada.Value.TimeOfDay);
            var saida = NormalizarHorario(dataPonto, model.Saida.Value.TimeOfDay, entrada);
            var inicioIntervalo = NormalizarHorario(dataPonto, model.InicioIntervalo.Value.TimeOfDay, entrada);
            var fimIntervalo = NormalizarHorario(dataPonto, model.FimIntervalo!.Value.TimeOfDay, entrada);

            if (inicioIntervalo < entrada || fimIntervalo <= inicioIntervalo || fimIntervalo > saida)
            {
                ModelState.AddModelError(nameof(model.InicioIntervalo), "O intervalo deve estar entre a entrada e a saída do cooperado.");
            }
        }

        // Somente cooperados com alocação no contrato selecionado podem ter ponto cadastrado.
        if (contratoId.HasValue && contratoId.Value != Guid.Empty && model.CooperadoId != Guid.Empty && model.Data != default)
        {
            var dataPonto = new DateTime(model.Data.Year, model.Data.Month, model.Data.Day, 0, 0, 0, DateTimeKind.Utc);

            var alocado = await _context.Alocacoes.AnyAsync(a =>
                a.CooperadoId == model.CooperadoId
                && a.ContratoId == contratoId.Value
                && a.DataInicio.Date <= dataPonto
                && (a.DataFim == null || a.DataFim >= dataPonto));

            if (!alocado)
            {
                ModelState.AddModelError(nameof(model.CooperadoId), "O cooperado não possui alocação neste contrato na data do ponto.");
            }
        }

        // Um cooperado só pode ter um registro de ponto por data (inclui a edição: o próprio
        // registro é ignorado pela busca, permitindo manter a data já utilizada por ele).
        if (model.CooperadoId != Guid.Empty && model.Data != default)
        {
            // A coluna é "timestamp with time zone": a comparação é feita por intervalo de dias.
            var diaDoPonto = new DateTime(model.Data.Year, model.Data.Month, model.Data.Day, 0, 0, 0, DateTimeKind.Utc);

            var jaPossuiRegistro = await _context.RegistrosPonto.AnyAsync(r =>
                r.CooperadoId == model.CooperadoId
                && r.Data >= diaDoPonto
                && r.Data < diaDoPonto.AddDays(1)
                && (registroIdIgnorado == null || r.Id != registroIdIgnorado));

            if (jaPossuiRegistro)
            {
                ModelState.AddModelError(nameof(model.Data), MensagemPontoDuplicado);
            }
        }
    }

    // Violação do índice único do banco (SQLSTATE 23505): a regra de unicidade é garantida
    // pelo PostgreSQL mesmo em corrida entre requisições; converte em mensagem amigável.
    private static bool EhViolacaoDeUnicidade(DbUpdateException ex) =>
        ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation;

    private static void NormalizarDatas(RegistroPonto registro)
    {
        // Normaliza a data para UTC para consistência com o resto da aplicação
        var dataPonto = new DateTime(registro.Data.Year, registro.Data.Month, registro.Data.Day, 0, 0, 0, DateTimeKind.Utc);
        registro.Data = dataPonto;
        registro.Entrada = registro.Entrada.HasValue ? dataPonto.Add(registro.Entrada.Value.TimeOfDay) : null;
        registro.Saida = registro.Saida.HasValue && registro.Entrada.HasValue
            ? NormalizarHorario(dataPonto, registro.Saida.Value.TimeOfDay, registro.Entrada.Value)
            : null;
        registro.InicioIntervalo = registro.InicioIntervalo.HasValue && registro.Entrada.HasValue
            ? NormalizarHorario(dataPonto, registro.InicioIntervalo.Value.TimeOfDay, registro.Entrada.Value)
            : null;
        registro.FimIntervalo = registro.FimIntervalo.HasValue && registro.Entrada.HasValue
            ? NormalizarHorario(dataPonto, registro.FimIntervalo.Value.TimeOfDay, registro.Entrada.Value)
            : null;
    }

    private static DateTime NormalizarHorario(DateTime data, TimeSpan horario, DateTime entrada)
    {
        var resultado = data.Add(horario);
        return resultado < entrada ? resultado.AddDays(1) : resultado;
    }
}
}