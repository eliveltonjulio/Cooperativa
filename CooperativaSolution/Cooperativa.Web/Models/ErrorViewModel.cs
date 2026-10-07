namespace Cooperativa.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    /// Indica que a exceção que originou esta página foi uma falha de conexão
    /// com o banco de dados (Npgsql/sockets) — exibido como orientação na view.
    /// </summary>
    public bool FalhaBanco { get; set; }

    /// <summary>
    /// Descrição curta da situação da ConnectionStrings__DefaultConnection
    /// no momento (ausente, valor localhost, não aplicada no deploy ou OK).
    /// </summary>
    public string? MotivoConnectionStrings { get; set; }
}
