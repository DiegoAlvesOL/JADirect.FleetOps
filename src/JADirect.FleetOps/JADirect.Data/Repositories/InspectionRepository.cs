using JADirect.Data.Infrastructure;
using JADirect.Domain.Models;
using MySql.Data.MySqlClient;

namespace JADirect.Data.Repositories;

/// <summary>
/// Repositório especializado na persistência de inspeções veiculares (Walkaround Checks).
/// </summary>
public class InspectionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InspectionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    } 

    public void Add(int userId, int vehicleId, int odometer, string checklistJson, bool hasDefect, string defectNotes, decimal? latitude, decimal? longitude )
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
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
    /// Recupera o histórico completo de inspeções de um veículo específico.
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
            history.Add(MapWalkaroundHistoryFromReader(reader));
        }
        return history;
    }

    /// <summary>
    /// Recupera o histórico de todas as inspeções realizadas na frota.
    /// </summary>
    public List<WalkaroundHistoryViewModel> GetAllHistory()
    {
        var historyList = new List<WalkaroundHistoryViewModel>();
        using var connection = _connectionFactory.CreateConnection();
        
        const string sqlWalkHistory = @"
            SELECT
                wc.check_date, u.first_name, u.surname, v.registration_no,
                wc.odometer, wc.has_defect, wc.defect_notes, wc.latitude, wc.longitude
            FROM walkaround_checks wc
            INNER JOIN users u ON wc.user_id = u.id
            INNER JOIN vehicles v ON wc.vehicle_id = v.id
            ORDER BY wc.check_date DESC";

        using var command = new MySqlCommand(sqlWalkHistory, (MySqlConnection)connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            historyList.Add(MapWalkaroundHistoryFromReader(reader));
        }
        return historyList;
    }

    /// <summary>
    /// Método Helper Centralizado para mapear o resultado do banco para o ViewModel de Histórico.
    /// </summary>
    private WalkaroundHistoryViewModel MapWalkaroundHistoryFromReader(MySqlDataReader reader)
    {
        return new WalkaroundHistoryViewModel
        {
            CheckDate = Convert.ToDateTime(reader["check_date"]),
            DriverName = string.Format("{0} {1}", reader["first_name"], reader["surname"]),
            RegistrationNo = reader["registration_no"].ToString() ?? "N/A",
            Odometer = Convert.ToInt32(reader["odometer"]),
            hasDefect = Convert.ToBoolean(reader["has_defect"]),
            DefectNotes = reader["defect_notes"]?.ToString(),
            Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]) : null,
            Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]) : null,
        };
    }
}