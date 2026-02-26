namespace JADirect.Domain.Models;

/// <summary>
/// Modelo de visualização para o dashboard gerencial. 
/// Transporta os totais consolidados e calcula índices de produtividade.
/// </summary>
public class PerformanceReportViewModel
{
    // Intervalo de tempo do relatório
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Dados brutos vindos do Repositório
    public int TotalDeliveries { get; set; }
    public int TotalCollections { get; set; }
    public int TotalReturns { get; set; }
    public int TotalKmTraveled { get; set; }
    
    /// <summary>
    /// Propriedade calculada: Soma todas as ações e divide pela distância.
    /// Define a "eficiência" que será exibida no topo do dashboard.
    /// </summary>
    public decimal EfficiencyIndex => TotalKmTraveled > 0 
    ? (decimal)(TotalDeliveries + TotalCollections + TotalReturns)/TotalKmTraveled
    : 0;
}