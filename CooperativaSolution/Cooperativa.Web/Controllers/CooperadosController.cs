using System.Text.RegularExpressions;
using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

[Route("[controller]")]
public class CooperadosController : Controller
{
    private readonly CooperativaDbContext _context;
    private readonly ILogger<CooperadosController> _logger;

    public CooperadosController(CooperativaDbContext context, ILogger<CooperadosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string busca, string status)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            var cooperadoIdStr = User.FindFirst("CooperadoId")?.Value;
            if (Guid.TryParse(cooperadoIdStr, out var meuId))
            {
                return RedirectToAction(nameof(Details), new { id = meuId });
            }
        }

        var cooperados = _context.Cooperados.AsQueryable();

        // Se coordenador tiver equipe definida, filtra por equipe
        var ehCoordenador = User.IsInRole("Coordenador") || User.IsInRole("Gestor");
        var equipeCoordenador = User.FindFirst("Equipe")?.Value;
        if (ehCoordenador && !string.IsNullOrWhiteSpace(equipeCoordenador))
        {
            cooperados = cooperados.Where(c => c.Equipe == equipeCoordenador || c.Equipe == null || c.Equipe == "");
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            cooperados = cooperados.Where(c => c.Nome.Contains(termo) || c.CPF.Contains(termo) || c.Email.Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "todos")
        {
            var ativo = status == "ativo";
            cooperados = cooperados.Where(c => c.Ativo == ativo);
        }

        var lista = await cooperados
            .OrderBy(c => c.Nome)
            .ToListAsync();

        ViewBag.Busca = busca;
        ViewBag.Status = status;
        return View(lista);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem cadastrar cooperados.";
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(Cooperado model)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem cadastrar cooperados.";
            return RedirectToAction(nameof(Index));
        }
        // Telefone/UF: normaliza o que foi digitado antes de validar
        // (ex.: 31988883333 -> (31) 98888-3333 e uf "mg" -> "MG").
        NormalizarDados(model);

        // Datas não anuláveis: o [Required] não detecta valor vazio, validar aqui.
        if (model.DataNascimento == default)
        {
            ModelState.AddModelError(nameof(model.DataNascimento), "A data de nascimento é obrigatória.");
        }

        if (model.DataAdmissao == default)
        {
            ModelState.AddModelError(nameof(model.DataAdmissao), "A data de admissão é obrigatória.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Campos opcionais chegam como null (binding converte vazio para null),
        // mas as colunas são NOT NULL no banco.
        PreencherStringsNulas(model);

        model.Id = Guid.NewGuid();
        model.DataAdmissao = DateTimeHelper.NormalizeToUtc(model.DataAdmissao, DateTime.UtcNow);
        model.Ativo = true;
        model.DataNascimento = DateTimeHelper.NormalizeToUtc(model.DataNascimento, DateTime.MinValue);

        _context.Cooperados.Add(model);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao cadastrar o cooperado {Cpf}.", model.CPF);
            TempData["MensagemErro"] = "Não foi possível salvar o cooperado. Verifique se o CPF informado já está cadastrado e tente novamente.";
            return View(model);
        }

        TempData["MensagemSucesso"] = "Cooperado cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            var cooperadoIdStr = User.FindFirst("CooperadoId")?.Value;
            if (Guid.TryParse(cooperadoIdStr, out var meuId) && meuId != id)
            {
                TempData["MensagemErro"] = "Você só pode visualizar o seu próprio cadastro.";
                return RedirectToAction(nameof(Details), new { id = meuId });
            }
        }

        var cooperado = await _context.Cooperados
            .Include(c => c.Contrato)
            .Include(c => c.Funcao)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cooperado == null ? NotFound() : View(cooperado);
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem alterar cadastro, inclusive o seu próprio.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cooperado = await _context.Cooperados.FirstOrDefaultAsync(c => c.Id == id);
        if (cooperado == null)
        {
            return NotFound();
        }

        return View(cooperado);
    }

    [HttpPost("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Cooperado model)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem alterar cadastro, inclusive o seu próprio.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cooperado = await _context.Cooperados.FirstOrDefaultAsync(c => c.Id == id);
        if (cooperado == null)
        {
            return NotFound();
        }

        NormalizarDados(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Usar SetValues é mais eficiente e menos propenso a erros do que atribuir cada propriedade manualmente.
        // Ele copia todos os valores escalares do objeto 'model' para a entidade 'cooperado' rastreada.
        var contratoIdOriginal = cooperado.ContratoId;
        var funcaoIdOriginal = cooperado.FuncaoId;

        // Campos opcionais vazios chegam como null; normalizar antes de copiar.
        PreencherStringsNulas(model);

        _context.Entry(cooperado).CurrentValues.SetValues(model);

        // Contrato e função foram retirados das telas de Cooperados;
        // preservar os vínculos originais para não perdê-los ao salvar.
        cooperado.ContratoId = contratoIdOriginal;
        cooperado.FuncaoId = funcaoIdOriginal;

        // As datas precisam de tratamento especial para garantir que sejam UTC.
        cooperado.DataNascimento = DateTimeHelper.NormalizeToUtc(model.DataNascimento, DateTime.MinValue);
        cooperado.DataAdmissao = DateTimeHelper.NormalizeToUtc(model.DataAdmissao == default ? cooperado.DataAdmissao : model.DataAdmissao, cooperado.DataAdmissao);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao atualizar o cooperado {Id}.", id);
            TempData["MensagemErro"] = "Não foi possível atualizar o cooperado. Revise os dados e tente novamente.";
            return View(model);
        }

        TempData["MensagemSucesso"] = "Cooperado atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem excluir cadastro, inclusive o seu próprio.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cooperado = await _context.Cooperados.FindAsync(id);
        if (cooperado == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Cooperados",
            DisplayName = cooperado.Nome,
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost("Delete/{id:guid}")]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var ehCooperado = User.IsInRole("Cooperado") && !User.IsInRole("Administrador") && !User.IsInRole("Admin") && !User.IsInRole("Coordenador") && !User.IsInRole("Gestor");
        if (ehCooperado)
        {
            TempData["MensagemErro"] = "Usuários com perfil cooperado não podem excluir cadastro, inclusive o seu próprio.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cooperado = await _context.Cooperados.FirstOrDefaultAsync(c => c.Id == id);
        if (cooperado == null)
        {
            return NotFound();
        }

        _context.Cooperados.Remove(cooperado);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Cooperado excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private static void PreencherStringsNulas(Cooperado model)
    {
        model.NomePai ??= string.Empty;
        model.NomeMae ??= string.Empty;
        model.Naturalidade ??= string.Empty;
        model.Escolaridade ??= string.Empty;
        model.Nacionalidade ??= string.Empty;
        model.Sexo ??= string.Empty;
        model.NumeroIdentidade ??= string.Empty;
        model.OrgaoEmissorIdentidade ??= string.Empty;
        model.CertificadoReservista ??= string.Empty;
        model.NumeroPis ??= string.Empty;
        model.TituloEleitor ??= string.Empty;
        model.ZonaEleitoral ??= string.Empty;
        model.SecaoEleitoral ??= string.Empty;
        model.Email ??= string.Empty;
        model.Telefone ??= string.Empty;
        model.Logradouro ??= string.Empty;
        model.Bairro ??= string.Empty;
        model.Cidade ??= string.Empty;
        model.Uf ??= string.Empty;
        model.Banco ??= string.Empty;
        model.ContaCorrente ??= string.Empty;
        model.Agencia ??= string.Empty;
        model.ChavePix ??= string.Empty;
        model.CEP ??= string.Empty;
        model.EstadoCivil ??= string.Empty;
        model.NomeConjuge ??= string.Empty;
        model.RegimeCasamento ??= string.Empty;
    }

    /// <summary>
    /// Normaliza os campos digitados no formulário antes da validação:
    /// telefone no formato (00) 00000-0000 e UF em maiúsculas.
    /// </summary>
    private void NormalizarDados(Cooperado model)
    {
        model.Nome = (model.Nome ?? string.Empty).Trim();
        model.Uf = (model.Uf ?? string.Empty).Trim().ToUpperInvariant();
        model.Telefone = FormatarTelefone(model.Telefone);

        // O telefone é digitado livremente (somente dígitos ou já com máscara):
        // o erro do valor bruto é descartado e o valor formatado é revalidado.
        ModelState.Remove(nameof(Cooperado.Telefone));
        ValidarTelefone(model);
    }

    /// <summary>Formata o telefone para (00) 00000-0000 a partir dos dígitos informados.</summary>
    private static string FormatarTelefone(string? valor)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        return digitos.Length switch
        {
            11 => $"({digitos[..2]}) {digitos.Substring(2, 5)}-{digitos.Substring(7, 4)}",
            10 => $"({digitos[..2]}) {digitos.Substring(2, 4)}-{digitos.Substring(6, 4)}",
            _ => (valor ?? string.Empty).Trim()
        };
    }

    private void ValidarTelefone(Cooperado model)
    {
        if (string.IsNullOrWhiteSpace(model.Telefone))
        {
            ModelState.AddModelError(nameof(model.Telefone), "O telefone é obrigatório.");
            return;
        }

        if (!Regex.IsMatch(model.Telefone, @"^\(\d{2}\) \d{4,5}-\d{4}$"))
        {
            ModelState.AddModelError(nameof(model.Telefone), "Informe o telefone no formato (00) 00000-0000.");
        }
    }
}
