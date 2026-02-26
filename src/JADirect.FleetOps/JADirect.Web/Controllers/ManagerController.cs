using JADirect.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;


/// <summary>
/// Responsável pela gestão e visualização de dados operacionais da frota.
/// Acesso restrito apenas para usuários com Role 'Manager'.
/// </summary>
[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly DailyLogRepository _dailyLogRepository;

    /// <summary>
    /// Construtor que recebe as dependências do módulo Daily Log.
    /// </summary>
    /// <param name="dailyLogRepository"></param>
    public ManagerController(DailyLogRepository dailyLogRepository)
    {
        _dailyLogRepository = dailyLogRepository;
    }

    /// <summary>
    ///Renderiza a visão principal do dashboard gerencial com os KPIs consolidados.
    /// Caso as datas não sejam informadas, utiliza o padrão de 7 dias.
    /// </summary>
    /// <param name="start">Data de início do filtro.</param>
    /// <param name="end">Data de fim do filtro.</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Index(DateTime? start, DateTime? end)
    {
        DateTime startDate = start ?? DateTime.Now.AddDays(-7);
        DateTime endDate = end ?? DateTime.Now;
        
        var report = _dailyLogRepository.GetDashboardTotals(startDate, endDate);
        
        return View(report);
    }
}