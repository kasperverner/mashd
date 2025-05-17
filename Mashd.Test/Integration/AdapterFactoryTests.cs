using Mashd.Backend.Adapters;
using Mashd.Test.Fixtures;
using Npgsql;

namespace Mashd.Test.Integration;

public class AdapterFactoryTests(CsvFixture csv, PostgreSqlFixture db)
    : IClassFixture<CsvFixture>, IClassFixture<PostgreSqlFixture>
{
    private readonly CsvFixture _csv = csv;
    private readonly PostgreSqlFixture _db = db;

    [Fact]
    public void CreateAdapter_CsvAdapter_Returns_CsvAdapter()
    {
        var config = new Dictionary<string, string>
        {
            { "source", _csv.TemporaryFilePath },
            { "delimiter", "," }
        };

        var adapter = AdapterFactory.CreateAdapter("csv", config);
            
        Assert.IsType<CsvAdapter>(adapter);
    }
    
    [Fact]
    public async Task UseAdapter_CsvAdapter_Returns_Data()
    {
        var config = new Dictionary<string, string>
        {
            { "source", _csv.TemporaryFilePath },
            { "delimiter", "," }
        };

        var adapter = AdapterFactory.CreateAdapter("csv", config);
        
        var data = (await adapter.ReadAsync()).ToArray();
        
        Assert.NotEmpty(data);
        Assert.Equal(10, data.Length);
    }

    [Fact]
    public async Task UseAdapter_CsvAdapter_Without_Delimiter_Returns_Data()
    {
        var config = new Dictionary<string, string>
        {
            { "source", _csv.TemporaryFilePath }
        };

        var adapter = AdapterFactory.CreateAdapter("csv", config);
        
        var data = (await adapter.ReadAsync()).ToArray();
        
        Assert.NotEmpty(data);
        Assert.Equal(10, data.Length);
    }
    
    [Fact]
    public void CreateAdapter_PostgreSqlAdapter_Returns_PostgreSqlAdapter()
    {
        var config = new Dictionary<string, string>
        {
            { "source", _db.ConnectionString },
            { "query", "SELECT * FROM Test" }
        };

        var adapter = AdapterFactory.CreateAdapter("postgresql", config);

        Assert.IsType<PostgreSqlAdapter>(adapter);
    }
    
    [Fact]
    public async Task UseAdapter_PostgreSqlAdapter_Returns_Data()
    {
        var config = new Dictionary<string, string>
        {
            { "source", _db.ConnectionString },
            { "query", "SELECT * FROM Test" }
        };

        var adapter = AdapterFactory.CreateAdapter("postgresql", config);

        var data = (await adapter.ReadAsync()).ToArray();
        
        Assert.NotEmpty(data);
        Assert.Equal(10, data.Length);
    }
    
    [Fact]
    public async Task UseAdapter_PostgreSqlAdapter_With_Invalid_ConnectionString_Throws_PostgresException()
    {
        var invalidConnectionString = _db.ConnectionString.Replace("mashd", "invalid");
        
        var config = new Dictionary<string, string>
        {
            { "source", invalidConnectionString },
            { "query", "SELECT * FROM Test" }
        };

        var adapter = AdapterFactory.CreateAdapter("postgresql", config);

        await Assert.ThrowsAsync<PostgresException>(async () => await adapter.ReadAsync());
    }
}