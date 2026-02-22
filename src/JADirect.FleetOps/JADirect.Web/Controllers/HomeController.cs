using System.Diagnostics;
using JADirect.Data.Services;
using JADirect.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using JADirect.Web.Models;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller principal que serve como o Funil Operacional do Motorista.
/// Gerencia o estado da sessão e a conformidade do veículo selecionado.
/// </summary>
public class HomeController : Controller
{
    private readonly FleetService _fleetService;

    public HomeController(FleetService fleetService)
    {
        _fleetService = fleetService;
    }

    /// <summary>
    /// Renderiza a Home baseada no estado do veículo selecionado na sessão.
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        //1. Primeiro verifica se o login é de manager ou driver, para o correto redirecionamento.
        if (User.IsInRole("Manager"))
        {
            return View("ManagerDashboard");
        }
        
        //2. Verificação de Sessão (Redireciona se não houver veículo selecionado)
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        string registrationNo = HttpContext.Session.GetString("SelectedVehicleRegistrationNo");

        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        
        // 3. Consumo do serviço de inteligência para o Semáforo Visual
        VehicleStatusResult status = _fleetService.GetWalkaroundStatus(vehicleId.Value);
        
        
        // Passamos a placa para a View via ViewBag
        ViewBag.RegistrationNo = registrationNo;

        return View(status);
    }

    /// <summary>
    /// Action padrão de erro do template ASP.NET
    /// </summary>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}