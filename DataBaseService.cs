using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Logging;

namespace ClassicExtended
{
    public class DataBaseService
    {
        private readonly ILogger<DataBaseService> _logger;
        private readonly string _connectionString;
        private readonly ClassicExtendedConfig _config;

        public DataBaseService(ClassicExtendedConfig config)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<DataBaseService>();

            _config = config;
            _connectionString = BuildDatabaseConnectionString();
        }

        private string BuildDatabaseConnectionString()
        {
            if (string.IsNullOrWhiteSpace(_config.DatabaseHost) || 
                string.IsNullOrWhiteSpace(_config.DatabaseUser) || 
                string.IsNullOrWhiteSpace(_config.DatabasePassword) || 
                string.IsNullOrWhiteSpace(_config.DatabaseName) ||
                _config.DatabasePort == 0)
            {
                throw new InvalidOperationException("Database is not set in the configuration file.");
            }

            MySqlConnectionStringBuilder builder = new()
            {
                Server = _config.DatabaseHost,
                Port = (uint)_config.DatabasePort,
                UserID = _config.DatabaseUser,
                Password = _config.DatabasePassword,
                Database = _config.DatabaseName,
                Pooling = true,
            };

            return builder.ConnectionString;
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while opening database connection");
                throw;
            }
        }
        public async Task TestAndCheckDataBaseTableAsync()
        {
            try
            {
                await using var connection = await GetOpenConnectionAsync();

                _logger.LogInformation("Database connection successful!");

                // USERS table
                string createUsersTableQuery = @"
                    CREATE TABLE IF NOT EXISTS `users` (
                        `SteamID` BIGINT PRIMARY KEY,
                        `Name` VARCHAR(255) NOT NULL DEFAULT 'Unknown',
                        `Points` BIGINT NOT NULL DEFAULT 0,
                        `Kills` INT NOT NULL DEFAULT 0,
                        `Deaths` INT NOT NULL DEFAULT 0,
                        `Assists` INT NOT NULL DEFAULT 0,
                        `Headshots` INT NOT NULL DEFAULT 0,
                        `TotalPlaytime` BIGINT NOT NULL DEFAULT 0,
                        `LastConnected` BIGINT NOT NULL DEFAULT 0
                    );";
                await connection.ExecuteAsync(createUsersTableQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking or creating tables");
                throw;
            }
        }

        public async Task ExecuteMultipleSqlWithTransactionAsync(IEnumerable<(string sql, object param)> queries)
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var query in queries)
                {
                    await connection.ExecuteAsync(query.sql, query.param, transaction: transaction);
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error while executing multiple SQL commands within a transaction");
                throw;
            }
        }
        public async Task<T?> ExecuteSqlAsync<T>(string sql, object param)
        {
            try
            {
                await using var connection = await GetOpenConnectionAsync();

                var result = await connection.QueryAsync<T>(sql, param);

                if (!result.Any())
                {
                    _logger.LogWarning("SQL command returned no results");
                    return default;
                }

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing SQL command");
                throw;
            }
        }
    }
}