using JADirect.Data.Repositories;
using JADirect.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JADirect.Domain.Models;
using JADirect.Domain.Enums;

namespace JADirect.Web.Controllers;

/// <summary>
/// Responsável pela gestão e visualização de dados operacionais da frota.
/// Acesso restrito apenas para usuários com Role 'Manager'.
/// </summary>
[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly DailyLogRepository _dailyLogRepository;
    private readonly FleetService _fleetService;

    public ManagerController(DailyLogRepository dailyLogRepository)
    {
        _dailyLogRepository = dailyLogRepository;
        _fleetService = new FleetService(); 
    }

    /// <summary>
    /// Renderiza o Dashboard. Acionado pela rota /Manager/Index.
    /// </summary>
    [HttpGet]
    public IActionResult Index(DateTime? start, DateTime? end, string? driverName)
    {
        DateTime startDate = start ?? DateTime.Now.AddDays(-7);
        DateTime endDate = end ?? DateTime.Now;

        var report = _dailyLogRepository.GetDashboardTotals(startDate, endDate);
        report.DriverSearch = driverName;
        
        _dailyLogRepository.FillDashboardDetails(report);
        _dailyLogRepository.FillComplianceExceptions(report); 
        
        // Recupera a lista de tuplas (Veículo + Nome do Motorista)
        var fleetData = _dailyLogRepository.GetAllVehiclesForComplianceCheck();

        foreach (var data in fleetData)
        {
            var v = data.Vehicle;
            var lastDriver = data.LastDriver;

            // ÚNICA CHAMADA: FleetService processa a regra de negócio
            var compliance = _fleetService.GetVehicleCompliance(
                v.Id, v.RegistrationNo, v.Manufacturer, v.Model, 
                v.CurrentKm, v.LastWalkaroundAt, v.VehicleType, (int)v.Status);

            // 1. Safety Alerts (Critical) -> Veículos Bloqueados (Status 4)
            if (v.Status == VehicleStatus.Blocked)
            {
                report.PendingWalkarounds.Add(new ComplianceExceptionViewModel {
                    VehicleId =  v.Id,
                    RegistrationNo = v.RegistrationNo,
                    DriverName = lastDriver, // Preenche o motorista que causou o bloqueio
                    Message = compliance.StatusMessage,
                    Severity = "danger"
                });
            }
            // 2. Inspection Status (Upcoming) -> Red (Expirado) ou Yellow (A vencer)
            else if (compliance.StatusColor == "Red" || compliance.StatusColor == "Yellow")
            {
                report.ExpiringInspections.Add(new ComplianceExceptionViewModel {
                    RegistrationNo = v.RegistrationNo,
                    DriverName = lastDriver, // Preenche o último motorista que inspecionou
                    Message = compliance.StatusMessage,
                    Severity = compliance.StatusColor == "Red" ? "danger" : "warning"
                });
            }
        }
        
        return View(report);
    }
}