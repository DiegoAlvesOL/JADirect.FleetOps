using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller responsável pela execução e registro das inspeções de segurança (Walkaround).
/// </summary>
public class WalkaroundController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehiclesId");
        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(IFormCollection form)
    {
        //1. Captura os dados básicos
        int vahicleId = HttpContext.Session.GetInt32("SelectedVehiclesId") ?? 0;
        int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        
        //2. Serialização Dinâmica (Critério de Aceite: System.Text.Json)
        //Agrupo todas as respostas do checklist em um dicionário para virar JSON
        var checklistAnswers = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            // Filtrando apenas os campos do checklist
            if (key.StartsWith("chk_"))
            {
                checklistAnswers.Add(key.Replace("chk_", ""), form[key]);
            }
            
        }

        string checklistJson = JsonSerializer.Serialize(checklistAnswers);

        // 3. Lógica de Defeitos (Se houver algum "Fail", marcamos has_defect)
        bool hasDefect = checklistAnswers.Values.Any(v => v == "Fail");

        return RedirectToAction("Index", "Home");
    }
}