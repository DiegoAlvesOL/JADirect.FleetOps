using JADirect.Application.Services;
using JADirect.Data.Repositories;
using JADirect.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller responsável pelo walkaround check.
/// Não contém lógica de negócio, toda a inteligência está no WalkaroundService.
/// </summary>
public class WalkaroundController : Controller
{
    private readonly WalkaroundService _walkaroundService;
    private readonly ChecklistItemRepository _checklistItemRepository;
    private readonly InspectionRepository _inspectionRepository;
    private readonly VehicleRepository _vehicleRepository;

    
    private const int JaDirectTenantId = 1;

    /// <summary>
    /// Construtor que recebe as dependências via Injeção de Dependência.
    /// </summary>
    public WalkaroundController(
        WalkaroundService walkaroundService,
        ChecklistItemRepository checklistItemRepository,
        InspectionRepository inspectionRepository,
        VehicleRepository vehicleRepository)
    {
        _walkaroundService = walkaroundService;
        _checklistItemRepository = checklistItemRepository;
        _inspectionRepository = inspectionRepository;
        _vehicleRepository = vehicleRepository;
    }

    /// <summary>
    /// Exibe o formulário de walkaround com os itens corretos para o tipo do veículo.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");

        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }

        // Carrega o veículo para obter o tipo e buscar os itens corretos do checklist
        var vehicle = _vehicleRepository.GetById(vehicleId.Value);

        if (vehicle == null)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }

        int vehicleTypeId = (int)vehicle.VehicleType;
        var checklistItems = _checklistItemRepository
            .GetItemsByVehicleType(JaDirectTenantId, vehicleTypeId);

        return View(checklistItems);
    }

    /// <summary>
    /// Processa o envio do formulário delegando ao WalkaroundService.
    /// Recebe a lista de resultados via model binding indexado.
    /// </summary>
    /// <param name="items">Lista de resultados dos itens preenchidos pelo motorista.</param>
    /// <param name="odometer">Leitura do odômetro informada no formulário.</param>
    /// <param name="latitude">Latitude capturada via GPS. Pode ser nula.</param>
    /// <param name="longitude">Longitude capturada via GPS. Pode ser nula.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        List<ChecklistItemResult> items,
        int odometer,
        decimal? latitude,
        decimal? longitude)
    {
        int vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId") ?? 0;
        int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

        if (vehicleId == 0 || userId == 0)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        
        var (vehicleBlocked, errorMessage) = _walkaroundService.SubmitInspection(
            userId,
            vehicleId,
            JaDirectTenantId,
            odometer,
            items,
            latitude,
            longitude);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            // Recarrega a View com os itens em caso de erro de validação
            var vehicle = _vehicleRepository.GetById(vehicleId);
            int vehicleTypeId = vehicle != null ? (int)vehicle.VehicleType : 1;
            var checklistItems = _checklistItemRepository
                .GetItemsByVehicleType(JaDirectTenantId, vehicleTypeId);

            ModelState.AddModelError("", errorMessage);
            return View(checklistItems);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Exibe o histórico de inspeções. Acesso exclusivo para Managers.
    /// </summary>
    /// <param name="id">ID do veículo. Se nulo, exibe o histórico de toda a frota.</param>
    [Authorize(Roles = "Manager")]
    [HttpGet]
    public IActionResult History(int? id)
    {
        List<WalkaroundHistoryViewModel> historyData;

        if (id.HasValue)
        {
            historyData = _inspectionRepository.GetHistoryByVehicleId(id.Value);
            var vehicle = _vehicleRepository.GetById(id.Value);
            ViewBag.RegistrationNo = vehicle != null ? vehicle.RegistrationNo : "Selected Vehicle";
        }
        else
        {
            historyData = _inspectionRepository.GetAllHistory();
            ViewBag.RegistrationNo = "All Fleet Vehicles";
        }

        return View(historyData);
    }
}