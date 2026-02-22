using System.Data.Common;
using JADirect.Data.Infrastructure;
using JADirect.Domain.Models;
using MySql.Data.MySqlClient;

namespace JADirect.Data.Repositories;

/// <summary>
/// Repositório especializado na persistência de inspeções veiculares (Walkaround Checks).
/// Gerencia operações críticas de segurança e conformidade no banco de dados.
/// </summary>
public class InspectionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Inicializa uma nova instância do repositório com a factory de conexão configurada.
    /// </summary>
    /// <param name="connectionFactory">Gerenciador de conexões com o MySql.</param>
    public InspectionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    } 

    /// <summary>
    /// Registra uma nova inspeção no banco de dados e, em caso de defeito, bloqueia o veículo.
    /// Esta operação é executada dentro de uma transação atômica.
    /// </summary>
    /// <param name="userId">ID do motorista executor.</param>
    /// <param name="vehicleId">ID do veículo inspecionado.</param>
    /// <param name="odometer">Leitura atual do odômetro.</param>
    /// <param name="checklistJson">Dados serializados dos 27 itens de segurança.</param>
    /// <param name="hasDefect">Sinaliza se houve falha em algum item.</param>
    /// <param name="defectNotes">Descrição textual detalhada dos defeitos.</param>
    /// <param name="latitude">Latitude da captura GPS.</param>
    /// <param name="longitude">Longitude da captura GPS.</param>
    public void Add(int userId, int vehicleId, int odometer, string checklistJson, bool hasDefect, string defectNotes, decimal? latitude, decimal? longitude )
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        // Iniciando transação para garantir que o bloqueio do veículo seja vinculado à inspeção
        using var transaction = connection.BeginTransaction();
        try
        {
            //1. Registro da Inspeção (Tabela: walkaround_checks)
            const string sqlInsert = @"INSERT INTO walkaround_checks(check_date, user_id, vehicle_id, odometer, checklist_json, has_defect, defect_notes, latitude, longitude)
VALUES(NOW(), @userId, @vehicleId, @odometer, @checkListJson,  @hasDefect, @defectNotes, @latitude, @longitude)";

            using var commandInsert =
                new MySqlCommand(sqlInsert, (MySqlConnection)connection, (MySqlTransaction)transaction);
            commandInsert.Parameters.AddWithValue("userId", userId);
            commandInsert.Parameters.AddWithValue("vehicleId", vehicleId);
            commandInsert.Parameters.AddWithValue("odometer", odometer);
            commandInsert.Parameters.AddWithValue("checkListJson", checklistJson);
            commandInsert.Parameters.AddWithValue("hasDefect", hasDefect ? 1 : 0);
            commandInsert.Parameters.AddWithValue("defectNotes", (object?)defectNotes ?? DBNull.Value);
            commandInsert.Parameters.AddWithValue("latitude", (object?)latitude ?? DBNull.Value);
            commandInsert.Parameters.AddWithValue("longitude", (object?)longitude ?? DBNull.Value);
            commandInsert.ExecuteNonQuery();

            
            // 2. Lógica de Bloqueio (Tabela: vehicles)
            // Se houver defeito, bloqueamos o veículo (status_id = 4).
            // Se NÃO houver defeito, apenas selecionamos 1 (operação inofensiva) para não quebrar o fluxo da transação.
            string sqlUpdate = hasDefect 
                ? "UPDATE vehicles SET status_id = 4 WHERE id = @vehicleId" 
                : "SELECT 1";
            
            using var commandUpdate =
                new MySqlCommand(sqlUpdate, (MySqlConnection)connection, (MySqlTransaction)transaction);
            commandUpdate.Parameters.AddWithValue("vehicleId", vehicleId);
            commandUpdate.ExecuteNonQuery();
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    
    /// <summary>
    /// Recupera o histórico completo de inspeções de um veículo, incluindo os nomes dos motoristas e a placa.
    /// Utiliza INNER JOIN múltiplo para garantir a integridade dos dados na auditoria.
    /// </summary>
    /// <param name="vehicleId">ID do veículo consultado.</param>
    /// <returns>Lista de modelos formatados para o histórico.</returns>
    public List<WalkaroundHistoryViewModel> GetHistoryByVehicleId(int vehicleId)
    {
        var history = new List<WalkaroundHistoryViewModel>();
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT 
                wc.check_date, 
                u.first_name, 
                u.surname, 
                v.registration_no, 
                wc.odometer, 
                wc.has_defect, 
                wc.defect_notes, 
                wc.latitude, 
                wc.longitude 
            FROM walkaround_checks wc 
            INNER JOIN users u ON wc.user_id = u.id 
            INNER JOIN vehicles v ON wc.vehicle_id = v.id 
            WHERE wc.vehicle_id = @vehicleId 
            ORDER BY wc.check_date DESC";

        using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            history.Add(new WalkaroundHistoryViewModel
            {
                CheckDate = Convert.ToDateTime(reader["check_date"]),
                DriverName = $"{reader["first_name"]} {reader["surname"]}",
                RegistrationNo = reader["registration_no"].ToString(),
                Odometer = Convert.ToInt32(reader["odometer"]),
                hasDefect = Convert.ToBoolean(reader["has_defect"]),
                DefectNotes = reader["defect_notes"]?.ToString(),
                Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]) : null,
                Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]) : null,
            });
        }
        return history;
    }
}