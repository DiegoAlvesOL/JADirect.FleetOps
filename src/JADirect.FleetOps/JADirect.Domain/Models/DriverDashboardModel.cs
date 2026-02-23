using JADirect.Domain.Entities;

namespace JADirect.Domain.Models;

/// <summary>
/// Modelo consolidado para o Dashboard do motorista.
/// </summary>
public class DriverDashboardModel
{
    public IEnumerable<VehicleStatusViewModel> AvailableVehicles { get; set; } = new List<VehicleStatusViewModel>();
    public List<RecentActivityItem> RecentActivities { get; set; } = new List<RecentActivityItem>();
}

/// <summary>
/// Extende os dados do veículo com informações de conformidade (Semáforo).
/// </summary>
public class VehicleStatusViewModel
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int CurrentKm { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    
    // Lógica do Semáforo
    public string StatusColor { get; set; } = "Red"; // Red, Yellow, Green
    public bool IsDailyLogAllowed { get; set; } = false;
    public string StatusMessage { get; set; } = string.Empty;
}

public class RecentActivityItem
{
    public DateTime LogDate { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public int Deliveries { get; set; }
    public int Collections { get; set; }
    public int Returns { get; set; }
}