using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Controllers;

public class ContratosController : Controller
{
    private readonly CooperativaDbContext _context;

    public ContratosController(CooperativaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string busca)
    {
        var contratos = _context.Contratos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            contratos = contratos.Where(c => c.EmpresaNome.Contains(busca) || c.Cnpj.Contains(busca));
        }

        var lista = await contratos.OrderBy(c => c.EmpresaNome).ToListAsync();
        ViewBag.Busca = busca;
        return View(lista);
    }

    public IActionResult Create() => View(new Contrato());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contrato model)
    {
        NormalizarDados(model);
        LimparErrosDeCamposFormatados();

        if (!TryValidateModel(model))
        {
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.DataInicio = DateTimeHelper.NormalizeToUtc(model.DataInicio, DateTime.UtcNow);
        model.DataFim = DateTimeHelper.NormalizeNullableToUtc(model.DataFim);

        // A empresa contratante é cadastrada diretamente no contrato (nome, CNPJ,
        // endereço e contatos), portanto não há mais entidade Empresa separada.
        _context.Contratos.Add(model);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Contrato cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id);
        return contrato == null ? NotFound() : View(contrato);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id);
        return contrato == null ? NotFound() : View(contrato);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Contrato model)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id);
        if (contrato == null)
        {
            return NotFound();
        }

        NormalizarDados(model);
        LimparErrosDeCamposFormatados();

        if (!TryValidateModel(model))
        {
            return View(model);
        }

        contrato.EmpresaNome = model.EmpresaNome;
        contrato.Cnpj = model.Cnpj;
        contrato.DataInicio = DateTimeHelper.NormalizeToUtc(model.DataInicio, DateTime.UtcNow);
        contrato.DataFim = DateTimeHelper.NormalizeNullableToUtc(model.DataFim);
        contrato.ValorContrato = model.ValorContrato;
        contrato.Logradouro = model.Logradouro;
        contrato.Bairro = model.Bairro;
        contrato.Cidade = model.Cidade;
        contrato.Uf = model.Uf;
        contrato.Cep = model.Cep;
        contrato.Contato1Nome = model.Contato1Nome;
        contrato.Contato1Telefone = model.Contato1Telefone;
        contrato.Contato2Nome = model.Contato2Nome;
        contrato.Contato2Telefone = model.Contato2Telefone;

        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Contrato atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarDados(Contrato model)
    {
        model.EmpresaNome = (model.EmpresaNome ?? string.Empty).Trim();
        model.Cnpj = FormatarCnpj(model.Cnpj);
        model.Logradouro = (model.Logradouro ?? string.Empty).Trim();
        model.Bairro = (model.Bairro ?? string.Empty).Trim();
        model.Cidade = (model.Cidade ?? string.Empty).Trim();
        model.Uf = (model.Uf ?? string.Empty).Trim().ToUpperInvariant();
        model.Cep = FormatarCep(model.Cep);
        model.Contato1Nome = (model.Contato1Nome ?? string.Empty).Trim();
        model.Contato1Telefone = FormatarTelefone(model.Contato1Telefone);
        model.Contato2Nome = (model.Contato2Nome ?? string.Empty).Trim();
        model.Contato2Telefone = FormatarTelefone(model.Contato2Telefone);
    }

    private void LimparErrosDeCamposFormatados()
    {
        // Remove erros de vinculação/formato dos campos normalizados para que
        // TryValidateModel reaplique a validação sobre os valores já formatados.
        ModelState.Remove(nameof(Contrato.Cnpj));
        ModelState.Remove(nameof(Contrato.Cep));
        ModelState.Remove(nameof(Contrato.Contato1Telefone));
        ModelState.Remove(nameof(Contrato.Contato2Telefone));
    }

    private static string SomenteDigitos(string? valor)
        => new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Mantém apenas letras e números (maiúsculos) — usado no CNPJ alfanumérico.</summary>
    private static string SomenteAlfanumericos(string? valor)
        => new string((valor ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string FormatarCnpj(string? valor)
    {
        // O CNPJ alfanumérico mantém a máscara 00.000.000/0000-00: os 12 primeiros
        // caracteres aceitam letras e números e os 2 dígitos verificadores são numéricos.
        var caracteres = SomenteAlfanumericos(valor);
        if (caracteres.Length == 14)
        {
            var digitosVerificadores = new string(caracteres[12..].Where(char.IsDigit).ToArray());
            if (digitosVerificadores.Length == 2)
            {
                return $"{caracteres[..2]}.{caracteres.Substring(2, 3)}.{caracteres.Substring(5, 3)}/{caracteres.Substring(8, 4)}-{digitosVerificadores}";
            }
        }

        return (valor ?? string.Empty).Trim();
    }

    private static string FormatarCep(string? valor)
    {
        var digitos = SomenteDigitos(valor);
        if (digitos.Length == 8)
        {
            return $"{digitos[..5]}-{digitos.Substring(5, 3)}";
        }

        return (valor ?? string.Empty).Trim();
    }

    private static string FormatarTelefone(string? valor)
    {
        var digitos = SomenteDigitos(valor);
        if (digitos.Length == 0)
        {
            // Contatos são opcionais: sem dígitos informados o campo permanece vazio.
            return string.Empty;
        }

        if (digitos.Length == 11)
        {
            return $"({digitos[..2]}) {digitos.Substring(2, 5)}-{digitos.Substring(7, 4)}";
        }

        if (digitos.Length == 10)
        {
            return $"({digitos[..2]}) {digitos.Substring(2, 4)}-{digitos.Substring(6, 4)}";
        }

        return (valor ?? string.Empty).Trim();
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id);
        if (contrato == null)
        {
            return NotFound();
        }

        var model = new Cooperativa.Web.Models.DeleteConfirmationViewModel
        {
            ControllerName = "Contratos",
            DisplayName = $"{contrato.EmpresaNome} ({contrato.DataInicio:dd/MM/yyyy})",
            RouteValues = new() { { "id", id } }
        };

        return PartialView("_DeleteConfirmation", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.Id == id);
        if (contrato == null) return NotFound();

        _context.Contratos.Remove(contrato);
        await _context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Contrato excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
