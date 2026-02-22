using System.Text.Json;
using JADirect.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller responsável pela execução e registro das inspeções de segurança (Walkaround).
/// </summary>
public class WalkaroundController : Controller
{
    private readonly InspectionRepository _inspectionRepository;
    private readonly VehicleRepository _vehicleRepository;

    /// <summary>
    /// Construtor que recebe as dependências necessárias via Injeção de Dependência.
    /// </summary>
    /// <param name="inspectionRepository">Repositório de acesso ao banco para inspeções.</param>
    public WalkaroundController(InspectionRepository inspectionRepository, VehicleRepository vehicleRepository)
    {
        _inspectionRepository = inspectionRepository;
        _vehicleRepository = vehicleRepository;
    }
    
    
    /// <summary>
    /// Exibe a tela de criação de uma nova inspeção de segurança.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Create()
    {
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        return View();
    }
    
    /// <summary>
    /// /// Procesa o envio do formulário de inspeção, gerencia a lógica de defeitos e persiste os dados.
    /// </summary>
    /// <param name="form">Coleção de dados enviados pelo formulário.</param>
    /// <param name="latitude">Latitude capturada via JS.</param>
    /// <param name="longitude">Longitude capturada via JS.</param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(IFormCollection form, decimal? latitude, decimal? longitude)
    {
        // Recuperação de contexto da sessão
        int vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId") ?? 0;
        int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        DateTime completionDate = DateTime.Now;
        
        //Captura de dados obrigatórios do formulário
        int odometer = int.Parse(form["Odometer"]);
        string defectNotes = form["DefectNotes"];
        
        // Filtragem e Serialização JSON das respostas (Critério de Aceite)
        var checklistAnswers = form.Keys
            .Where(k => k.StartsWith("chk_"))
            .ToDictionary(k => k.Replace("chk_", ""), k => form[k].ToString());
        string json = JsonSerializer.Serialize(checklistAnswers);
        
        // Definição do estado de risco do veículo
        bool hasDefect = checklistAnswers.Values.Any(v => v =="Fail");
        
        // Persistência via Repositório (SQL + Transação)
        _inspectionRepository.Add(userId, vehicleId, odometer, json, hasDefect, defectNotes, latitude, longitude);

        if (!hasDefect)
        {
            _vehicleRepository.UpdateLastInspectionDate(vehicleId, completionDate);
        }
        return RedirectToAction("Index", "Home");
    }
}