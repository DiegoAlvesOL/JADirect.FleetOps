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
    /// Renderiza a visão principal do dashboard gerencial.
    /// Consolida KPIs de topo, ranking de performance e auditoria de logs em uma única chamada.
    /// </summary>
    /// <param name="start">Data de início do filtro (Opcional).</param>
    /// <param name="end">Data de fim do filtro (Opcional).</param>
    /// <param name="driverName">Nome para filtro de busca textual (Opcional).</param>
    /// <returns>View com o modelo PerformanceReportViewModel preenchido.</returns>
    [HttpGet]
    public IActionResult Index(DateTime? start, DateTime? end, string? driverName)
    {
        DateTime startDate = start ?? DateTime.Now.AddDays(-7);
        DateTime endDate = end ?? DateTime.Now;

        var report = _dailyLogRepository.GetDashboardTotals(startDate, endDate);

        report.DriverSearch = driverName;
        
        _dailyLogRepository.FillDashboardDetails(report);
        
        return View(report);
    }
    
}