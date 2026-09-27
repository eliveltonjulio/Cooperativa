namespace Cooperativa.Web.Models;

public class DeleteConfirmationViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = "Delete";
    public string DisplayName { get; set; } = string.Empty;
    public string Title { get; set; } = "Confirmar exclusão";
    public string Message { get; set; } = "Tem certeza que deseja excluir este item?";

    /// <summary>
    /// Para chaves primárias compostas, contém os valores de rota.
    /// </summary>
    public Dictionary<string, object> RouteValues { get; set; } = new();
}