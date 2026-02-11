namespace JADirect.Domain.Entities;
/// <summary>
/// Representa uma inspeção de segurança completa (Walkaround Check).
/// Este objeto armazena o resultado dos 27 itens verificados pelo motorista.
/// </summary>
public class WalkaroundCheck
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public int Odometer { get; set; }
    /// <summary>
    /// String em formato JSON contendo a lista de itens e seus respectivos estados (Pass/Fail).
    /// Exemplo: [{"item": "Tyres", "status": "Pass"}, ...]
    /// </summary>
    public string CheckListJSON { get; set; }
    public bool HasDefect { get; set; }
    public string? DefectNotes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}