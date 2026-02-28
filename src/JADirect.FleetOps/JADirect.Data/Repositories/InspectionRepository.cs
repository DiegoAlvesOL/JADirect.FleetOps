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
    /// Registra uma nova inspeção e atualiza o estado do veículo (Data e Status) em uma transação única.
    /// </summary>
    /// <param name="userId">ID do motorista executor.</param>
    /// <param name="vehicleId">ID do veículo inspecionado.</param>
    /// <param name="odometer">Leitura atual do odômetro.</param>
    /// <param name="checklistJson">Dados serializados dos itens de segurança.</param>
    /// <param name="hasDefect">Sinaliza se houve falha em algum item.</param>
    /// <param name="defectNotes">Descrição textual detalhada dos defeitos.</param>
    /// <param name="latitude">Latitude da captura GPS.</param>
    /// <param name="longitude">Longitude da captura GPS.</param>
    public void Add(int userId, int vehicleId, int odometer, string checklistJson, bool hasDefect, string defectNotes, decimal? latitude, decimal? longitude )
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Registro da Inspeção (Tabela: walkaround_checks)
            const string sqlInsert = @"INSERT INTO walkaround_checks(check_date, user_id, vehicle_id, odometer, checklist_json, has_defect, defect_notes, latitude, longitude)
                                       VALUES(NOW(), @userId, @vehicleId, @odometer, @checkListJson, @hasDefect, @defectNotes, @latitude, @longitude)";

            using var commandInsert = new MySqlCommand(sqlInsert, (MySqlConnection)connection, (MySqlTransaction)transaction);
            commandInsert.Parameters.AddWithValue("userId", userId);
            commandInsert.Parameters.AddWithValue("vehicleId", vehicleId);
            commandInsert.Parameters.AddWithValue("odometer", odometer);
            commandInsert.Parameters.AddWithValue("checkListJson", checklistJson);
            commandInsert.Parameters.AddWithValue("hasDefect", hasDefect ? 1 : 0);
            commandInsert.Parameters.AddWithValue("defectNotes", (object?)defectNotes ?? DBNull.Value);
            commandInsert.Parameters.AddWithValue("latitude", (object?)latitude ?? DBNull.Value);
            commandInsert.Parameters.AddWithValue("longitude", (object?)longitude ?? DBNull.Value);
            commandInsert.ExecuteNonQuery();

            // 2. Atualização Atômica do Veículo (Tabela: vehicles)
            // Independentemente do resultado (Pass/Fail), a data da última inspeção é atualizada.
            // O status_id será 4 se houver defeito, ou 1 se o veículo estiver operacional.
            const string sqlUpdate = @"UPDATE vehicles 
                                       SET status_id = @statusId, 
                                           last_walkaround_at = NOW() 
                                       WHERE id = @vehicleId";
            
            using var commandUpdate = new MySqlCommand(sqlUpdate, (MySqlConnection)connection, (MySqlTransaction)transaction);
            commandUpdate.Parameters.AddWithValue("statusId", hasDefect ? 4 : 1);
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
    /// Recupera o histórico completo de inspeções de um veículo para auditoria.
    /// </summary>
    public List<WalkaroundHistoryViewModel> GetHistoryByVehicleId(int vehicleId)
    {
        var history = new List<WalkaroundHistoryViewModel>();
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT 
                wc.check_date, u.first_name, u.surname, v.registration_no, 
                wc.odometer, wc.has_defect, wc.defect_notes, wc.latitude, wc.longitude 
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
    
    /// <summary>
    /// Busca a data da inspeção mais recente para o semáforo do Dashboard.
    /// </summary>
    public DateTime? GetLastInspectionDate(int vehicleId)
    {
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        const string sql = "SELECT MAX(check_date) FROM walkaround_checks WHERE vehicle_id = @vehicleId";
        
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        
        connection.Open();
        var result = command.ExecuteScalar();
        return result == DBNull.Value ? null : (DateTime?)result;
    }

    public List<WalkaroundHistoryViewModel> GetAllHistory()
    {
        var historyList = new List<WalkaroundHistoryViewModel>();
        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sqlWalkHistory = @"
                SELECT
                    wc.check_date, u.first_name, u.surname, v.registration_no,
                    wc.odometer, wc.has_defect, wc.defect_notes,  wc.latitude, wc.longitude
                FROM walkaround_checks wc
                INNER JOIN users u ON wc.user_id = u.id
                INNER JOIN vehicles v ON wc.vehicle_id = v.id
                ORDER BY wc.check_date DESC";

            using (var command = new MySqlCommand(sqlWalkHistory, (MySqlConnection)connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        historyList.Add(new WalkaroundHistoryViewModel
                        {
                            CheckDate = Convert.ToDateTime(reader["check_date"]),
                            DriverName = string.Format("{0} {1}", reader["first_name"], reader["surname"]),
                            RegistrationNo = reader["registration_no"].ToString(),
                            Odometer = Convert.ToInt32(reader["odometer"]),
                            hasDefect = Convert.ToBoolean(reader["has_defect"]),
                            DefectNotes = reader["defect_notes"]?.ToString(),
                            Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]) : null,
                            Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]) : null,
                        });
                    }
                }
            }
        }
        return historyList;
    }
}