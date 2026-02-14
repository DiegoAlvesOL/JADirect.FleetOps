using System.Data;
using System.Data.Common;
using JADirect.Data.Infrastructure;
using JADirect.Domain.Entities;
using JADirect.Domain.Enums;

namespace JADirect.Data.Repositories;


/// <summary>
/// Repositório responsável pela persistência de dados dos usuários no MySql.
/// </summary>
public class UserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Insere um novo usuário no banco de dados.
    /// </summary>
    /// <param name="user"></param>
    public void Add(User user)
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sql =
                @"INSERT INTO users(first_name, surname, email, phone_number, password_hash, role_id, status_id, created_at) 
                VALUES(@FirstName, @Surname, @Email, @PhoneNumber, @PasswordHash, @RoleId, @StatusId, @CreatedAt)";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                AddParameter(command, "@FirstName", user.FirstName);
                AddParameter(command, "@Surname", user.Surname);
                AddParameter(command, "@Email", user.Email);
                AddParameter(command, "@PhoneNumber", user.PhoneNumber);
                AddParameter(command, "@PasswordHash", user.PasswordHash);
                AddParameter(command, "@RoleId", (int)user.Role);
                AddParameter(command, "@StatusId", (int)user.Status);
                AddParameter(command, "@CreatedAt", user.CreatedAt);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Busca um usuário pelo e-mail único.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public User GetByEmail(string email)
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sql = "SELECT * FROM users WHERE email = @Email";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                AddParameter(command, "@Email", email);
                
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapUserFromReader((DbDataReader)reader);
                    }
                }
            }
            
        }

        return null;
    }

    /// <summary>
    /// Helper para adicionar parâmetros de forma segura a ideia é evitar SQL Injection.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    private void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Mapeia o resultado do banco de dados (IDataReader) para a entidade de domínio User.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private User MapUserFromReader(DbDataReader reader)
    {
        return new User()
        {
            Id = Convert.ToInt32(reader["id"]),
            FirstName = reader["first_name"].ToString(),
            Surname = reader["surname"].ToString(),
            Email = reader["email"].ToString(),
            PhoneNumber = reader["phone_number"].ToString(),
            PasswordHash = reader["password_hash"].ToString(),
            Role = (UserRoles)Convert.ToInt32(reader["role_id"]),
            Status = (UserStatus)Convert.ToInt32(reader["status_id"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"])
        };
    }

    /// <summary>
    /// Lista todos os usuário gravados no banco.
    /// </summary>
    /// <returns>Lista completa de usuários.</returns>
    public List<User> GetAll()
    {
        var users = new List<User>();
        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sql = "SELECT * FROM users ORDER BY first_name ASC";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                connection.Open();
                using (var reader = (System.Data.Common.DbDataReader)command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(MapUserFromReader(reader));
                    }
                }
            }
        }
        return users;
    }

    /// <summary>
    /// Atualiza o status de um usuário para 'Canceled'.
    /// </summary>
    /// <param name="userId">ID do usuário a ser desativado.</param>
    public void Deactivate(int userId)
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            const string sql = "UPDATE users SET status_id = @StatusId WHERE id = @Id";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                AddParameter(command, "@StatusId", (int)UserStatus.Canceled);
                AddParameter(command, "@Id", userId);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        
    }
}