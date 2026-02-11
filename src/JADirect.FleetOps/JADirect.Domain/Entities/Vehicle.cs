using JADirect.Domain.Enums;

namespace JADirect.Domain.Entities;

/// <summary>
/// Representação de um veículo fisico da frota da JA Direct.
/// Contém informações de identificação, categoria e estado de conservação.
/// </summary>
public class Vehicle
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public VehicleType VehicleType { get; set; }
    public int CurrentKM { get; set; }
    public VehicleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
