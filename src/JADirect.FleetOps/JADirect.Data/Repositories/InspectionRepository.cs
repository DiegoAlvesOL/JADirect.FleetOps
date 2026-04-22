using JADirect.Data.Infrastructure;
using JADirect.Domain.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

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
    
    /// <summary>
    /// Persiste uma nova inspeção de walkaround e atualiza o status do veículo.
    /// O status do veículo e a decisão de bloqueio são calculados pelo WalkaroundService
    /// antes desta chamada. O repositório apenas grava o que o serviço decidiu.
    /// </summary>
    /// <param name="userId">ID do motorista.</param>
    /// <param name="vehicleId">ID do veículo inspecionado.</param>
    /// <param name="odometer">Leitura do odômetro.</param>
    /// <param name="checklistJson">JSON serializado com o resultado de cada item.</param>
    /// <param name="vehicleStatusId">Status calculado pelo WalkaroundService: 4=bloqueado, 1=operacional.</param>
    /// <param name="latitude">Latitude GPS. Pode ser nula.</param>
    /// <param name="longitude">Longitude GPS. Pode ser nula.</param>
    public void Add(
        int userId,
        int vehicleId,
        int odometer,
        string checklistJson,
        int vehicleStatusId,
        decimal? latitude,
        decimal? longitude)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        try
        {
            // has_defect é inferido do vehicleStatusId para manter compatibilidade
            // com registros históricos que ainda leem esta coluna.
            // Coluna defect_notes removida pois as notas agora ficam por item no JSON.
            const string sqlInsert = @"
            INSERT INTO walkaround_checks
                (check_date, user_id, vehicle_id, odometer, checklist_json,
                 has_defect, latitude, longitude)
            VALUES
                (NOW(), @userId, @vehicleId, @odometer, @checklistJson,
                 @hasDefect, @latitude, @longitude)";
            
            using var commandInsert = new MySqlCommand(
                sqlInsert,
                (MySqlConnection)connection,
                (MySqlTransaction)transaction);

            commandInsert.Parameters.AddWithValue("userId", userId);
            commandInsert.Parameters.AddWithValue("vehicleId", vehicleId);
            commandInsert.Parameters.AddWithValue("odometer", odometer);
            commandInsert.Parameters.AddWithValue("checklistJson", checklistJson);
            // has_defect é true quando o veículo foi bloqueado (status_id = 4)
            commandInsert.Parameters.AddWithValue("hasDefect", vehicleStatusId == 4 ? 1 : 0);
            commandInsert.Parameters.AddWithValue("latitude", (object?)latitude ?? DBNull.Value);
            commandInsert.Parameters.AddWithValue("longitude", (object?)longitude ?? DBNull.Value);
            commandInsert.ExecuteNonQuery();
            
            // Atualiza o status e a data do último walkaround no veículo
            const string sqlUpdate = @"
            UPDATE vehicles
            SET status_id = @statusId,
                last_walkaround_at = NOW()
            WHERE id = @vehicleId";

            using var commandUpdate = new MySqlCommand(
                sqlUpdate,
                (MySqlConnection)connection,
                (MySqlTransaction)transaction);
            
            commandUpdate.Parameters.AddWithValue("statusId", vehicleStatusId);
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
    /// <param name="vehicleId">ID do veículo.</param>
    /// <returns>Lista de WalkaroundHistoryViewModel com itens individuais desserializados.</returns>
    public List<WalkaroundHistoryViewModel> GetHistoryByVehicleId(int vehicleId)
    {
        var history = new List<WalkaroundHistoryViewModel>();
        using var connection = _connectionFactory.CreateConnection();

        // ALTERADO: checklist_json incluído no SELECT para desserialização por item.
        // has_defect e defect_notes removidos pois o status agora é calculado
        // a partir dos itens pelo WalkaroundHistoryViewModel.VehicleWasBlocked.
        const string sql = @"
        SELECT
            wc.check_date, u.first_name, u.surname, v.registration_no,
            wc.odometer, wc.checklist_json, wc.latitude, wc.longitude
        FROM walkaround_checks wc
        INNER JOIN users u ON wc.user_id = u.id
        INNER JOIN vehicles v ON wc.vehicle_id = v.id
        WHERE wc.vehicle_id = @vehicleId
        ORDER BY wc.check_date DESC
        LIMIT 30";

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
    /// <returns>Lista de WalkaroundHistoryViewModel com itens individuais desserializados.</returns>
    public List<WalkaroundHistoryViewModel> GetAllHistory()
    {
        var historyList = new List<WalkaroundHistoryViewModel>();
        using var connection = _connectionFactory.CreateConnection();

        // ALTERADO: checklist_json incluído no SELECT para desserialização por item.
        const string sqlWalkHistory = @"
        SELECT
            wc.check_date, u.first_name, u.surname, v.registration_no,
            wc.odometer, wc.checklist_json, wc.latitude, wc.longitude
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
    
    private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// Helper privado centralizado para mapear o resultado do banco
    /// para o WalkaroundHistoryViewModel com itens individuais desserializados.
    /// </summary>
    /// <param name="reader">Reader posicionado em um registro válido.</param>
    /// <returns>WalkaroundHistoryViewModel preenchido com itens do checklist.</returns>
    private WalkaroundHistoryViewModel MapWalkaroundHistoryFromReader(MySqlDataReader reader)
    {
        var checklistJson = reader["checklist_json"]?.ToString() ?? "[]";

        // Desserializa o JSON gravado pelo WalkaroundService em lista de itens.
        // O try/catch protege contra registros malformados sem quebrar o histórico completo.
        var items = new List<ChecklistItemResult>();

        try
        {
            items = JsonSerializer.Deserialize<List<ChecklistItemResult>>(
                checklistJson,
                JsonReadOptions) ?? new List<ChecklistItemResult>();
        }
        catch
        {
            // Registro malformado: retorna lista vazia para não quebrar o histórico.
            items = new List<ChecklistItemResult>();
        }

        return new WalkaroundHistoryViewModel
        {
            CheckDate = Convert.ToDateTime(reader["check_date"]),
            DriverName = string.Format("{0} {1}", reader["first_name"], reader["surname"]),
            RegistrationNo = reader["registration_no"].ToString() ?? string.Empty,
            Odometer = Convert.ToInt32(reader["odometer"]),
            Items = items,
            Latitude = reader["latitude"] != DBNull.Value
                ? Convert.ToDecimal(reader["latitude"])
                : null,
            Longitude = reader["longitude"] != DBNull.Value
                ? Convert.ToDecimal(reader["longitude"])
                : null
        };
    }
}