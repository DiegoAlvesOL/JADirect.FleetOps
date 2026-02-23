using JADirect.Data.Repositories;
using JADirect.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controller responsável pelo registro de métricas operacionais (Entregas/Coletas/Retornos).
/// Gerencia a entrada de dados do motorista e garante que o contexto (Veículo/Usuário) esteja correto.
/// </summary>
public class DailyLogController : Controller
{
    private readonly DailyLogRepository _repository;

    /// <summary>
    /// Construtor que recebe o repositório via Injeção de Dependência.
    /// </summary>
    /// <param name="repository">Instância do repositório de logs diários.</param>
    public DailyLogController(DailyLogRepository repository)
    {
        _repository = repository;
    }
    
    /// <summary>
    /// Exibe a tela de lançamento para o motorista.
    /// Verifica se um veículo foi previamente selecionado na sessão.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        
        // Se não houver veículo na sessão, obriga o motorista a selecionar um.
        if (vehicleId == null)
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        
        return View(new DailyLog());
    }

    /// <summary>
    /// Processa o envio do formulário de log diário.
    /// </summary>
    /// <param name="log">Objeto preenchido com os dados da View.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(DailyLog log)
    {
        // 1. Recuperar contexto do Veículo (armazenado na SESSION)
        var vehicleId = HttpContext.Session.GetInt32("SelectedVehicleId");
        
        // 2. Recuperar contexto do Usuário (armazenado nos CLAIMS/Autenticação)
        // Buscamos o Claim "UserId" que foi criado no AccountController durante o Login.
        var userIdClaim = User.FindFirst("UserId")?.Value;

        // VALIDAÇÃO DE SEGURANÇA: 
        // Se a sessão expirou ou o usuário não está devidamente identificado, interrompe o processo.
        if (vehicleId == null || string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction("SelectVehicle", "Driver");
        }
        
        // 3. Preenchimento de Metadados:
        // Vinculamos o Log ao veículo e usuário corretos antes de validar o modelo.
        log.VehicleId = vehicleId.Value;
        log.UserId = int.Parse(userIdClaim);
        log.LogDate = DateTime.Now;
        
        // Removemos a validação automática destes campos, pois os preenchemos via código acima.
        ModelState.Remove("UserId");
        ModelState.Remove("VehicleId");
        
        // 4. Validação de Regra de Negócio:
        // Impede que valores negativos de produtividade sejam enviados.
        if (log.Deliveries < 0 || log.Collections < 0 || log.Returns < 0)
        {
            ModelState.AddModelError("", "Operational values (Deliveries/Collections/Returns) cannot be negative.");
        }

        // 5. Persistência:
        // Se tudo estiver correto, chama o repositório para gravar no banco de dados.
        if (ModelState.IsValid)
        {
            _repository.Add(log);
            
            // Retorna para a Home após o sucesso.
            return RedirectToAction("Index", "Home");
        }

        // Se houver erro de validação, retorna para a mesma View exibindo as mensagens de erro.
        return View(log);
    }
}