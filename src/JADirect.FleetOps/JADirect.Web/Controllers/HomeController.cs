using System.Diagnostics;
using JADirect.Application.Services;
using JADirect.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using JADirect.Web.Models;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller principal que serve como o Funil Operacional do Motorista.
/// Gerencia o estado da sessão e exibe o status atual do veículo selecionado.
/// </summary>
public class HomeController : Controller
{
    private readonly FleetService _fleetService;
    private readonly VehicleRepository _vehicleRepository; // Adicionado para buscar os dados do veículo da sessão

    public HomeController(FleetService fleetService, VehicleRepository vehicleRepository)
    {
        _fleetService = fleetService;
        _vehicleRepository = vehicleRepository;
    }

    /// <summary>
    /// Renderiza a Home baseada no estado do veículo selecionado na sessão.
    /// </summary>
    public IActionResult Index()
    {
        // 1. Redirecionamento baseado no papel do usuário
        if (User.IsInRole("Manager"))
        {
            return RedirectToAction("Index", "Manager");
        }
        
        // 2. Verificação de Sessão (Redireciona para seleção se a sessão expirou ou não existe)
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        string registrationNo = HttpContext.Session.GetString("SelectedVehicleRegistrationNo");

        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        
        // 3. Busca os dados atuais do veículo no banco para validar o Semáforo
        // Precisamos dos dados completos (tipo, km, etc) para o FleetService processar
        var vehicle = _vehicleRepository.GetAll().FirstOrDefault(v => v.Id == vehicleId.Value);

        if (vehicle == null)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }

        // 4. Consumo do serviço de inteligência usando o novo método centralizado
        // Note que agora passamos todos os dados para que a regra de Van/Truck funcione
        var status = _fleetService.GetVehicleCompliance(
            vehicle.Id, 
            vehicle.RegistrationNo, 
            vehicle.Manufacturer, 
            vehicle.Model, 
            vehicle.CurrentKm, 
            vehicle.LastWalkaroundAt, 
            vehicle.VehicleType,
            (int)vehicle.Status
        );
        
        ViewBag.RegistrationNo = registrationNo;

        // Retornamos o status (VehicleStatusViewModel) para a View
        return View(status);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}