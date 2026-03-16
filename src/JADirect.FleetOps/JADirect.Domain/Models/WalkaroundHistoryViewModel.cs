namespace JADirect.Domain.Models;

/// <summary>
/// Modelo de visualização para o histórico de auditoria.
/// Combina dados da inspeção com dados do usuário (motorista).
/// </summary>
public class WalkaroundHistoryViewModel
{
    public DateTime CheckDate{ get; set; }
    public string DriverName { get; set; }
    public string RegistrationNo { get; set; }
    public int Odometer { get; set; }
    public bool hasDefect { get; set; }
    public string DefectNotes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    public bool IsPassed => !hasDefect;
    
    public string? Location => Latitude.HasValue && Longitude.HasValue ?
        $"https://www.google.com/maps?q={Latitude},{Longitude}" :
        null;
}