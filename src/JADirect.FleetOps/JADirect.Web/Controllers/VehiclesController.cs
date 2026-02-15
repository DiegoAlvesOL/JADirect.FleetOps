using JADirect.Data.Repositories;
using JADirect.Domain.Entities;
using JADirect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;


/// <summary>
/// Controlador responsável pelo inventário de veículos da JA Direct.
/// Acesso restrito a usuários com perfil 'Manager'
/// </summary>
[Authorize(Roles = "Manager")]
public class VehiclesController : Controller
{
    private readonly VehicleRepository _vehiclesRepository;

    public VehiclesController(VehicleRepository vehiclesRepository)
    {
        _vehiclesRepository = vehiclesRepository;
    }
    
    /// <summary>
    /// Lista todos os veículos da frota.
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        var fleet = _vehiclesRepository.GetAll();
        return View(fleet);
    }

    /// <summary>
    /// Abre a tela de cadastro de novo veículo.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Processa o cadastro de um novo ativo.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Vehicle vehicle)
    {
        ModelState.Remove("CreatedAt");
        ModelState.Remove("Status");

        if (vehicle.CurrentKm < 0)
        {
            ModelState.AddModelError("CurrentKm", "Initial mileage cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return View(vehicle);
        }

        if (_vehiclesRepository.Exists(vehicle.RegistrationNo))
        {
            ModelState.AddModelError("RegistrationNo", "This vehicle is already registered in the system.");
            return View(vehicle);
        }

        try
        {
            // Regra Todo veículo novo entra como 'Active' e com data atual
            vehicle.Status = VehicleStatus.Active;
            vehicle.CreatedAt = DateTime.Now;

            _vehiclesRepository.Add(vehicle);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Internal error while saving the vehicle. Please try again.");
            return View(vehicle);

        }
    }
}