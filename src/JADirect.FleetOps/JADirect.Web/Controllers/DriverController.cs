using JADirect.Data.Repositories;
using JADirect.Application.Services;
using JADirect.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Gerencia o Dashboard do motorista. 
/// Seu papel é orquestrador, delegando de acordo com cada serviços.
/// </summary>
[Authorize]
public class DriverController : Controller
{
    private readonly VehicleRepository _vehicleRepository;
    private readonly DailyLogRepository _dailyLogRepository;
    private readonly FleetService _fleetService; // Substituímos o cálculo manual por este serviço

    /// <summary>
    /// Injeção de dependências. Note que não precisamos mais do InspectionRepository aqui,
    /// pois o FleetService usa a data já mapeada no objeto Vehicle.
    /// </summary>
    public DriverController(VehicleRepository vehicleRepository, 
                            DailyLogRepository dailyLogRepository,
                            FleetService fleetService)
    {
        _vehicleRepository = vehicleRepository;
        _dailyLogRepository = dailyLogRepository;
        _fleetService = fleetService;
    }

    /// <summary>
    /// Carrega o Dashboard principal.
    /// Utiliza o FleetService para aplicar as regras de Van vs Caminhão em cada veículo.
    /// </summary>
    [HttpGet]
    public IActionResult SelectVehicle()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        var dashboardData = new DriverDashboardModel();

        if (!string.IsNullOrEmpty(userIdClaim))
        {
            int userId = int.Parse(userIdClaim);
            
            // 1. Busca apenas veículos ativos/operacionais
            var vehicles = _vehicleRepository.GetOperationalVehicles();
            
            // 2. Transforma a lista de veículos em ViewModels de Status usando o FleetService
            dashboardData.AvailableVehicles = vehicles.Select(v => 
                _fleetService.GetVehicleCompliance(
                    v.Id, 
                    v.RegistrationNo, 
                    v.Manufacturer, 
                    v.Model, 
                    v.CurrentKm, 
                    v.LastWalkaroundAt, 
                    v.VehicleType,
                    (int)v.Status
                )
            ).ToList();

            // 3. Carrega o histórico de produtividade do motorista
            dashboardData.RecentActivities = _dailyLogRepository.GetRecentLogs(userId);
        }
    
        return View(dashboardData);
    }

    /// <summary>
    /// Recebe a seleção do veículo e a intenção (Daily Log ou Walkaround).
    /// Salva os dados na sessão para uso nos próximos passos.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmVehicle(int vehicleId, string registrationNo, string actionType)
    {
        if (vehicleId <= 0 || string.IsNullOrEmpty(registrationNo))
        {
            return RedirectToAction("SelectVehicle");
        }

        // Armazena o veículo escolhido na sessão para persistência entre telas
        HttpContext.Session.SetInt32("SelectedVehicleId", vehicleId);
        HttpContext.Session.SetString("SelectedVehicleRegistrationNo", registrationNo);
        
        // Redirecionamento baseado na ação escolhida na View
        if (actionType == "DailyLog")
        {
            return RedirectToAction("Create", "DailyLog");
        }
            
        return RedirectToAction("Create", "Walkaround");
    }
}