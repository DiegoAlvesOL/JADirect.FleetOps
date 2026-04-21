namespace JADirect.Domain.Entities;

/// <summary>
/// Representa uma inspeção de segurança completa (Walkaround Check).
/// O status do veículo é calculado pelo WalkaroundService com base nas
/// regras do tenant, não armazenado como campo booleano nesta entidade.
/// </summary>
public class WalkaroundCheck
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public int Odometer { get; set; }

    /// <summary>
    /// Array JSON contendo o resultado de cada item inspecionado.
    /// Formato de cada elemento:
    /// {
    ///   "item": "tyres",
    ///   "state": "Defect",
    ///   "actionTaken": "Resolved",
    ///   "note": "Flat tyre, replaced on site"
    /// }
    /// Valores válidos para state: "Good", "Attention", "Defect".
    /// Valores válidos para actionTaken: "None", "Resolved", "RequiresGarage".
    /// Note é null quando state é "Good".
    /// </summary>
    public string CheckListJson { get; set; } = string.Empty;

    
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}