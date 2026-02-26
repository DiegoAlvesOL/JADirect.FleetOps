using System.Data;
using JADirect.Data.Infrastructure;
using JADirect.Domain.Entities;
using JADirect.Domain.Models; // Importante para usar o RecentActivityItem
using MySql.Data.MySqlClient;

namespace JADirect.Data.Repositories;

/// <summary>
/// Repositório especializado na persistência de dados de produtividade diária (Daily Logs).
/// Centraliza operações de inserção, histórico e relatórios gerenciais de agregação.
/// </summary>
public class DailyLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DailyLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Realiza a inserção de um novo registro de DailyLog na tabela 'daily_logs'.
    /// </summary>
    public void Add(DailyLog log)
    {
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO daily_logs (log_date, user_id, vehicle_id, deliveries, collections, returns, current_odometer, notes, created_at)
            VALUES (@LogDate, @UserId, @VehicleId, @Deliveries, @Collections, @Returns, @CurrentOdometer, @Notes, NOW())";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LogDate", log.LogDate);
        command.Parameters.AddWithValue("@UserId", log.UserId);
        command.Parameters.AddWithValue("@VehicleId", log.VehicleId);
        command.Parameters.AddWithValue("@Deliveries", log.Deliveries);
        command.Parameters.AddWithValue("@Collections", log.Collections);
        command.Parameters.AddWithValue("@Returns", log.Returns);
        command.Parameters.AddWithValue("@CurrentOdometer", (object?)log.CurrentOdometer ?? DBNull.Value);
        command.Parameters.AddWithValue("@Notes", (object?)log.Notes ?? DBNull.Value);
        
        connection.Open();
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Busca os registros recentes mapeados para a classe RecentActivityItem.
    /// </summary>
    public List<RecentActivityItem> GetRecentLogs(int userId, int limit = 5)
    {
        var logs = new List<RecentActivityItem>();
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT dl.log_date, v.registration_no, dl.deliveries, dl.collections, dl.returns 
            FROM daily_logs dl
            INNER JOIN vehicles v ON dl.vehicle_id = v.id
            WHERE dl.user_id = @UserId
            ORDER BY dl.log_date DESC
            LIMIT @Limit";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Limit", limit);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new RecentActivityItem
            {
                LogDate = reader.GetDateTime("log_date"),
                RegistrationNo = reader.GetString("registration_no"),
                Deliveries = reader.GetInt32("deliveries"),
                Collections = reader.GetInt32("collections"),
                Returns = reader.GetInt32("returns")
            });
        }
        return logs;
    }

    /// <summary>
    /// Realiza a soma agregada de produtividade em um período específico.
    /// Esta função é consumida pelo ManagerController para alimentar os KPIs de topo.
    /// </summary>
    /// <param name="startDate">Data inicial da busca.</param>
    /// <param name="endDate">Data final da busca.</param>
    /// <returns>Objeto PerformanceReportViewModel preenchido com os totais.</returns>
    public PerformanceReportViewModel GetDashboardTotals(DateTime startDate, DateTime endDate)
    {
        var report = new PerformanceReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
        };

        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                SUM(deliveries) as total_deliveries,
                SUM(collections) as total_collections,
                SUM(returns) as total_returns,
                (MAX(current_odometer) - MIN(current_odometer)) as total_km
            FROM daily_logs
            WHERE log_date BETWEEN @startDate AND @endDate";

        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@startDate", startDate.Date);
        command.Parameters.AddWithValue("@endDate", endDate.Date.AddDays(1).AddTicks(-1));
        
        connection.Open();
        using var reader = command.ExecuteReader();
        
        if (reader.Read())
        {
            report.TotalDeliveries = reader["total_deliveries"] != DBNull.Value ? Convert.ToInt32(reader["total_deliveries"]) : 0;
            report.TotalCollections = reader["total_collections"] != DBNull.Value ? Convert.ToInt32(reader["total_collections"]) : 0;
            report.TotalReturns = reader["total_returns"] != DBNull.Value ? Convert.ToInt32(reader["total_returns"]) : 0;
            report.TotalKmTraveled = reader["total_km"] != DBNull.Value ? Convert.ToInt32(reader["total_km"]) : 0;
        }
        return report;
    }
}