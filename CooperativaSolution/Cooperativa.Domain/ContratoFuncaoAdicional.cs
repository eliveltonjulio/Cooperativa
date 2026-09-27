namespace Cooperativa.Models;

public class ContratoFuncaoAdicional
{
    public Guid ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    public Guid FuncaoId { get; set; }
    public Funcao? Funcao { get; set; }

    public Guid AdicionalId { get; set; }
    public TipoAdicional? Adicional { get; set; }
}