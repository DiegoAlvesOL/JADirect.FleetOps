using JADirect.Data.Repositories;
using JADirect.Domain.Entities;

namespace JADirect.Application.Services;

/// <summary>
/// Orquestrador das regras de negócio do DailyLog.
/// É o único ponto autorizado a criar um novo log diário na aplicação.
/// </summary>
public class DailyLogService
{
    private readonly DailyLogRepository _repository;

    // Número máximo de dias no passado que um motorista pode registrar.
    private const int MaximumPastDaysAllowed = 7;

    
    public DailyLogService(DailyLogRepository repository)
    {
        _repository = repository;
    }

    public (bool Success, string ErrorMessage) CreateLog(DailyLog log)
    {
        // REGRA 1: A data escolhida não pode ser futura.
        // O motorista só pode registrar o dia atual ou dias anteriores.
        if (log.LogDate.Date > DateTime.Now.Date)
        {
            return (false, "ou cannot register a log for a future date.");
        }
        
        // REGRA 2: A data escolhida não pode ultrapassar a janela de 7 dias.
        // O calculo é quantos dias se passaram entre a data escolhida e hoje.
        int daysInThePast = (DateTime.Now.Date - log.LogDate).Days;

        if (daysInThePast > MaximumPastDaysAllowed)
        {
            return (false, $"You can only register logs up to {{MaximumPastDaysAllowed}} days in the past.");
        }
        
        // REGRA 3: Não pode existir um log para o mesmo motorista,
        // mesmo veículo e mesma data. Esta é a defesa contra duplicados.
        bool logAlreadyExists = _repository.HasLogForData(log.UserId, log.VehicleId, log.LogDate);

        if (logAlreadyExists)
        {
            return (false, "A log for this vehicle has already been submitted for the selected date.");
        }
        
        // Todas as regras passaram. Persiste o log no banco de dados.
        _repository.Add(log);
        
        return (true, string.Empty);
    }
}