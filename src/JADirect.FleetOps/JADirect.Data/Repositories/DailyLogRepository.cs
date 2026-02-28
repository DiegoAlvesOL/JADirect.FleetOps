using JADirect.Data.Infrastructure;
using JADirect.Domain.Entities;
using JADirect.Domain.Enums;
using JADirect.Domain.Models;
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

    /// <summary>
    /// Obtém a performance detalhada e o ranking de motoristas para o dashboard.
    /// </summary>
    public void FillDashboardDetails(PerformanceReportViewModel report)
    {
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        connection.Open();

        const string sqlRanking = @"
            SELECT
                CONCAT(u.first_name, ' ', u.surname) as driver_name,
                v.vehicle_type_id,
                v.registration_no,
                SUM(dl.deliveries) as total_deliveries,
                SUM(dl.collections) as total_collections,
                SUM(dl.returns) as total_returns
            FROM daily_logs dl
            INNER JOIN users u ON dl.user_id = u.id
            INNER JOIN vehicles v ON dl.vehicle_id = v.id
            WHERE dl.log_date BETWEEN @start AND @end
            GROUP BY u.id, v.vehicle_type_id, v.registration_no
            ORDER BY total_deliveries DESC";
        
        using var commandRanking = new MySqlCommand(sqlRanking, connection);
        commandRanking.Parameters.AddWithValue("@start", report.StartDate.Date);
        commandRanking.Parameters.AddWithValue("@end", report.EndDate.Date.AddDays(1).AddTicks(-1));

        using var readerRanking = commandRanking.ExecuteReader();
        while (readerRanking.Read())
        {
            report.DriverRanking.Add(new VehiclePerformanceSummary
            {
                DriverName = readerRanking["driver_name"].ToString() ?? "",
                VehicleType = readerRanking["vehicle_type_id"].ToString() ?? "",
                RegistrationNo = readerRanking["registration_no"].ToString() ?? "",
                Deliveries = Convert.ToInt32(readerRanking["total_deliveries"]),
                Collections = Convert.ToInt32(readerRanking["total_collections"]),
                Returns = Convert.ToInt32(readerRanking["total_returns"])
            });
        }
        readerRanking.Close();

        string sqlDetails = @"
            SELECT
                dl.log_date,
                CONCAT(u.first_name, ' ', u.surname) as driver_name,
                v.vehicle_type_id,
                v.registration_no,
                dl.deliveries,
                dl.collections,
                dl.returns
            FROM daily_logs dl
            INNER JOIN users u ON dl.user_id = u.id
            INNER JOIN vehicles v ON dl.vehicle_id = v.id
            WHERE dl.log_date BETWEEN @start AND @end";

        if (!string.IsNullOrEmpty(report.DriverSearch))
        {
            sqlDetails = sqlDetails + " HAVING driver_name LIKE @search";
        }

        sqlDetails = sqlDetails + " ORDER BY dl.log_date DESC";

        using var commandDetails = new MySqlCommand(sqlDetails, connection);
        commandDetails.Parameters.AddWithValue("@start", report.StartDate.Date);
        commandDetails.Parameters.AddWithValue("@end", report.EndDate.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrEmpty(report.DriverSearch))
        {
            commandDetails.Parameters.AddWithValue("@search", $"%{report.DriverSearch}%");
        }

        using var readerDetails = commandDetails.ExecuteReader();
        while (readerDetails.Read())
        {
            report.DetailedLogs.Add(new DailyLogDetailItem
            {
                LogDate = Convert.ToDateTime(readerDetails["log_date"]),
                DriverName = readerDetails["driver_name"].ToString() ?? "",
                VehicleType = readerDetails["vehicle_type_id"].ToString() ?? "",
                RegistrationNo = readerDetails["registration_no"].ToString() ?? "",
                Deliveries = Convert.ToInt32(readerDetails["deliveries"]),
                Collections = Convert.ToInt32(readerDetails["collections"]),
                Returns = Convert.ToInt32(readerDetails["returns"])
            });
        }
    }

    /// <summary>
    /// Identifica motoristas ativos que não enviaram logs na data atual.
    /// Visão: Pending Daily Logs.
    /// </summary>
    public void FillComplianceExceptions(PerformanceReportViewModel report)
    {
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        connection.Open();

        const string sqlMissingLogs = @"
        SELECT CONCAT_WS(' ', u.first_name, u.surname) as full_name
            FROM users u
        LEFT JOIN daily_logs dl ON u.id = dl.user_id AND DATE(dl.log_date) = CURDATE()
        WHERE u.role_id IN (2, 3)
            AND u.status_id = 1
            AND dl.id IS NULL
        ORDER BY u.first_name ASC";
    
        using var commandLogs = new MySqlCommand(sqlMissingLogs, connection);
        using var readerLogs = commandLogs.ExecuteReader();
        while (readerLogs.Read())
        {
            report.PendingDailyLogs.Add(new ComplianceExceptionViewModel
            {
                DriverName = readerLogs["full_name"].ToString() ?? "",
                Message = "No data received today",
                Severity = "warning"
            });
        }
    }

    /// <summary>
    /// Recupera a frota com a informação do último motorista que realizou a inspeção.
    /// Retorna uma lista de tuplas para evitar alteração na classe de domínio Vehicle.
    /// Chamado pelo ManagerController.Index.
    /// </summary>
    public List<(Vehicle Vehicle, string LastDriver)> GetAllVehiclesForComplianceCheck()
    {
        var data = new List<(Vehicle Vehicle, string LastDriver)>();
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();

        // Subquery baseada no seu user_id para buscar o nome do motorista do último check
        const string sql = @"
            SELECT 
                v.id, v.registration_no, v.manufacturer, v.model, 
                v.vehicle_type_id, v.current_km, v.status_id, v.last_walkaround_at,
                (SELECT CONCAT(u.first_name, ' ', u.surname) 
                 FROM walkaround_checks wc 
                 INNER JOIN users u ON wc.user_id = u.id 
                 WHERE wc.vehicle_id = v.id 
                 ORDER BY wc.check_date DESC LIMIT 1) as last_driver
            FROM vehicles v
            WHERE v.status_id != 3";
        
        using var command = new MySqlCommand(sql, connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var vehicle = new Vehicle {
                Id = Convert.ToInt32(reader["id"]),
                RegistrationNo = reader["registration_no"].ToString() ?? "",
                Manufacturer = reader["manufacturer"].ToString() ?? "",
                Model = reader["model"].ToString() ?? "",
                VehicleType = (VehicleType)Convert.ToInt32(reader["vehicle_type_id"]),
                Status = (VehicleStatus)Convert.ToInt32(reader["status_id"]),
                LastWalkaroundAt = reader["last_walkaround_at"] != DBNull.Value ? Convert.ToDateTime(reader["last_walkaround_at"]) : null
            };

            string driverName = reader["last_driver"] != DBNull.Value ? reader["last_driver"].ToString() : "No driver recorded";
            
            data.Add((vehicle, driverName));
        }
        return data;
    }
}