using Npgsql;
using Testcontainers.PostgreSql;

namespace Mashd.Test.Fixtures;

public class IntegrationPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _sqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("mashd")
        .WithUsername("postgres")
        .WithPassword("ch4ng3m3!")
        .Build();

    public string ConnectionString => _sqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await SeedDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    private async Task SeedDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string createTableSql = $"""
                                           CREATE TABLE IF NOT EXISTS Test (
                                               ID SERIAL PRIMARY KEY,
                                               FirstName TEXT NOT NULL,
                                               LastName TEXT NOT NULL
                                           );
                                       """;

        const string checkDataSql = $"""
                                         SELECT COUNT(*) FROM Test;
                                     """;
        
        const string insertDataSql = $"""
                                          INSERT INTO Test (FirstName, LastName) VALUES
                                          ('John', 'Doe'),
                                          ('Jane', 'Smith'),
                                          ('Alice', 'Johnson'),
                                          ('Bob', 'Brown'),
                                          ('Charlie', 'Davis'),
                                          ('Eve', 'Wilson'),
                                          ('Frank', 'Garcia'),
                                          ('Grace', 'Martinez'),
                                          ('Heidi', 'Lopez'),
                                          ('Ivan', 'Gonzalez');
                                      """;

        await using var createTableCommand = new NpgsqlCommand(createTableSql, connection);
        await createTableCommand.ExecuteNonQueryAsync();

        await using var checkDataCommand = new NpgsqlCommand(checkDataSql, connection);
        var rowCount = (long)(await checkDataCommand.ExecuteScalarAsync() ?? 0);
        
        if (rowCount == 0)
        {
            await using var insertDataCommand = new NpgsqlCommand(insertDataSql, connection);
            await insertDataCommand.ExecuteNonQueryAsync();
        }
        
        await connection.CloseAsync();
    }
}