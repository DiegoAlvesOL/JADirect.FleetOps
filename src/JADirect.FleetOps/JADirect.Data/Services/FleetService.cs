using JADirect.Data.Infrastructure;
using JADirect.Domain.Models;

namespace JADirect.Data.Services;
/// <summary>
/// Serviço responsável pela inteligência de conformidade da frota.
/// Gerencia as regras de negócio de tempo para inspeções (Walkaround) 
/// e determina o estado operacional dos veículos para a JA Direct.
/// </summary>
public class FleetService
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Construtor que recebe as conexões para operações em SQL.
    /// </summary>
    /// <param name="dbConnectionFactory"></param>
    public FleetService(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    /// <summary>
    /// Consulta o banco de dados e processa o estado de conformidade de um veículo específico.
    /// Utiliza o campo last_walkaround_at para otimização de performance.
    /// </summary>
    /// <param name="vehicleId"></param>
    /// <returns></returns>
    public VehicleStatusResult GetWalkaroundStatus(int vehicleId)
    {
        DateTime? lastWalkaroundAt =  null;

        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sql = "SELECT last_walkaround_at FROM vehicles WHERE id = @VehicleId";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@VehicleId";
                parameter.Value = vehicleId;
                command.Parameters.Add(parameter);
                
                connection.Open();
                var result = command.ExecuteScalar();

                if (result != DBNull.Value && result != null)
                {
                    lastWalkaroundAt = Convert.ToDateTime(result);
                }
            }
        }

        return CalculateStatus(lastWalkaroundAt);
    }


    /// <summary>
    /// Método privado que encapsula a lógica matemática e de negócio da JA Direct.
    /// Define as regras de 7 dias para expiração do Walkaround.
    /// </summary>
    /// <param name="lastWalkaroundAt">Data da última inspeção realizada.</param>
    /// <returns>Instância de VehicleStatusResult com os dados processados.</returns>
    private VehicleStatusResult CalculateStatus(DateTime? lastWalkaroundAt)
    {
        var result = new VehicleStatusResult
        {
            LastWalkaroundDate = lastWalkaroundAt
        };

        // Se o registro for nulo, o veículo nunca foi inspecionado (Risco Máximo)
        if (!lastWalkaroundAt.HasValue)
        {
            result.StatusColor = "Red";
            result.Message = "Vehicle has no recorded inspection. Action required immediately.";
            result.IsActionRequired = true;
            result.DaysSinceLastCheck = 0;
            return result;
        }
        
        
        // Cálculo de diferença utilizando nomenclatura rigorosa conforme instruído
        TimeSpan timeSinceLastInspection = DateTime.Now - lastWalkaroundAt.Value;
        int daysSinceLastInspection = (int)timeSinceLastInspection.TotalDays;
        
        result.DaysSinceLastCheck = daysSinceLastInspection;
        
        //Regra do Semáforo da J&A Direct (Intervalor de 7 dias)
        if (daysSinceLastInspection < 6)
        {
            result.StatusColor = "Green";
            result.Message = "Vehicle is compliant for operation";
            result.IsActionRequired = false;
        }
        
        else if (daysSinceLastInspection == 6)
        {
            result.StatusColor = "Yellow";
            result.Message = "Inspection expires tomorrow. Plan your check.";
            result.IsActionRequired = false;
        }

        else
        {
            result.StatusColor = "Red";
            result.Message = "Safety inspection EXPIRED. Operation blocked.";
            result.IsActionRequired = true;
        }
        return result;
    }
}