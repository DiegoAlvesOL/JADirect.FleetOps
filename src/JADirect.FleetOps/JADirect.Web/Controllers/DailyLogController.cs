using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller responsável pelo registro de métricas operacionais (Entregas/Coletas).
/// </summary>
public class DailyLogController : Controller
{
    /// <summary>
    /// Exibe o formulário de lançamento diário.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Create()
    {
        var vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        if (vehicleId == null)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(int deliveries, int collections, int returns, int? odometer)
    {
        // Validação rigorosa apenas para produtividade (critério de aceite)
        if (deliveries < 0 || collections < 0 || returns < 0)
        {
            ModelState.AddModelError("", "Operational values (Deliveries/Collections) cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        return RedirectToAction("Index", "Home");
    }
    
}