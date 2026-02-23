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
    /// <param name="vehicleRepository">Repositório de acesso ao banco para veículos.</param>
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
    /// Processa o envio do formulário de inspeção e delega a persistência unificada ao repositório.
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
        
        // Captura de dados obrigatórios do formulário
        int odometer = int.Parse(form["Odometer"]);
        string defectNotes = form["DefectNotes"];
        
        // LISTA DE CAMPOS FIXOS: Ignoramos estes para capturar apenas as perguntas do checklist
        var fixedFields = new[] { "Odometer", "DefectNotes", "__RequestVerificationToken", "Latitude", "Longitude" };

        // Filtragem e Serialização JSON das respostas
        var checklistAnswers = form.Keys
            .Where(k => !fixedFields.Contains(k))
            .ToDictionary(k => k, k => form[k].ToString());

        string json = JsonSerializer.Serialize(checklistAnswers);
        
        // Identifica se houve falha em algum item para definir o novo status do veículo
        bool hasDefect = checklistAnswers.Values.Any(v => v == "Fail");
        
        // O repositório agora executa o fluxo unificado: salva o log e atualiza o veículo (status e data)
        _inspectionRepository.Add(userId, vehicleId, odometer, json, hasDefect, defectNotes, latitude, longitude);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Exibe o histórico de auditoria para o veículo atualmente selecionado na sessão.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult History()
    {
        int? vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        if (!vehicleId.HasValue)
        {
            return RedirectToAction("SelectVehicle", "Home");
        }

        var history = _inspectionRepository.GetHistoryByVehicleId(vehicleId.Value);
        ViewBag.RegistrationNo = HttpContext.Session.GetString("SelectedVehicleRegistrationNo");
        
        return View(history);
    }
}