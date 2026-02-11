namespace JADirect.Domain.Entities;

/// <summary>
/// Registra a performance diária de entregas, coletas e retornos. 
/// </summary>
public class DailyLog
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public int Deliveries { get; set; }
    public int Collections { get; set; }
    public int Returns { get; set;  }
    public string? Notes { get; set;  }
    public DateTime CreatedAt { get; set; }
}