using JADirect.Domain.Entities;

namespace JADirect.Domain.Models;

public class VehicleManageViewModel
{
    public Vehicle Vehicle { get; set; }

    public List<WalkaroundHistoryViewModel> WalkaroundHistory { get; set; } = new();
}