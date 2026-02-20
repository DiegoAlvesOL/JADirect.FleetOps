using JADirect.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Este Controlller é focado nas operações do dia a dia do motorista.
/// </summary>
public class DriverController : Controller
{
    private readonly VehicleRepository _vehicleRepository;

    public DriverController(VehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    /// <summary>
    /// Tela de seleção de veículos ela interagem com o arquivos JADirect.Data/Repositories/VehicleRepository.cs
    /// para pegar apenas os vehículos com status de Active
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult SelectVehicle()
    {
        var activeVehicles = _vehicleRepository.GetOperationalVehicles();
        return View(activeVehicles);
    }

    /// <summary>
    /// Confirma a seleção do veículo e armazena os dados na sessão.
    /// </summary>
    /// <param name="vehicleId">O ID primário do veículo.</param>
    /// <param name="regNo">A placa do veículo para exibição rápida.</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ConfirmVehicle(int vehicleId, string registrationNo)
    {
        if (vehicleId <= 0 || string.IsNullOrEmpty(registrationNo))
        {
            return RedirectToAction("SelectVehicle");
        }
        // Armazenando na sessão para uso nos módulos de Walkaround e Daily Log
        HttpContext.Session.SetInt32("SelectedVehiclesId", vehicleId);
        HttpContext.Session.SetString("SelectedVehicleRegistrationNo", registrationNo);
        
        // Por enquanto, retorna à Home até criarmos o próximo card
        return RedirectToAction("Index", "Home");
    }
}