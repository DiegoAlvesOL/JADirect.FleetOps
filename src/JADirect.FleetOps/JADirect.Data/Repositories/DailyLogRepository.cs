using System.Data;
using JADirect.Data.Infrastructure;
using JADirect.Domain.Entities;
using MySql.Data.MySqlClient;

namespace JADirect.Data.Repositories;

/// <summary>
/// Repositório especializado na persistência de dados de produtividade diária (Daily Logs).
/// Gerencia a gravação de entregas, coletas e quilometragem no banco de dados MySql.
/// </summary>
public class DailyLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    /// <summary>
    /// Inicializa uma nova instância do repositório utilizando a fábrica de conexões do sistema.
    /// </summary>
    /// <param name="connectionFactory">Provedor de conexões com o banco de dados.</param>
    public DailyLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Realiza a inserção de um novo registro de DailyLog na tabela 'daily_logs'.
    /// Este método utiliza parâmetros para garantir a segurança contra SQL Injection.
    /// </summary>
    /// <param name="log">Objeto contendo os dados operacionais e de quilometragem da jornada.</param>
    public void Add(DailyLog log)
    {
        // Criação da conexão utilizando a factory injetada
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();

        // Query SQL estruturada para bater exatamente com as colunas da tabela daily_logs
        const string sql = @"
            INSERT INTO daily_logs (
                log_date, 
                user_id, 
                vehicle_id, 
                deliveries, 
                collections, 
                returns, 
                current_odometer, 
                notes, 
                created_at
            )
            VALUES (
                @LogDate, 
                @UserId, 
                @VehicleId, 
                @Deliveries, 
                @Collections, 
                @Returns, 
                @CurrentOdometer, 
                @Notes, 
                NOW()
            )";

        using var command = new MySqlCommand(sql, connection);
        
        // --------------------------------------------------------------------------------
        // MAPEAMENTO DE PARÂMETROS
        // --------------------------------------------------------------------------------
        
        // Dados obrigatórios vindos da operação
        command.Parameters.AddWithValue("@LogDate", log.LogDate);
        command.Parameters.AddWithValue("@UserId", log.UserId);
        command.Parameters.AddWithValue("@VehicleId", log.VehicleId);
        command.Parameters.AddWithValue("@Deliveries", log.Deliveries);
        command.Parameters.AddWithValue("@Collections", log.Collections);
        command.Parameters.AddWithValue("@Returns", log.Returns);
        
        // Campos Opcionais: 
        // Se o valor for nulo (null), enviamos DBNull.Value para o MySql gravar corretamente.
        command.Parameters.AddWithValue("@CurrentOdometer", (object?)log.CurrentOdometer ?? DBNull.Value);
        command.Parameters.AddWithValue("@Notes", (object?)log.Notes ?? DBNull.Value);
        
        // Abertura da conexão e execução do comando
        connection.Open();
        command.ExecuteNonQuery();
    }
}