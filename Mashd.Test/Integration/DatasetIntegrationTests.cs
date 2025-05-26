using Mashd.Backend;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;
using Mashd.Test.Fixtures;

namespace Mashd.Test.Integration;

public class DatasetIntegrationTests(CsvFixture csv, PostgreSqlFixture db, MashdFixture mashdFixture)
    : IClassFixture<CsvFixture>, IClassFixture<PostgreSqlFixture>, IClassFixture<MashdFixture>
{
    private readonly CsvFixture _csv = csv;
    private readonly PostgreSqlFixture _db = db;
    private readonly MashdFixture _mashdFixture = mashdFixture;

    [Fact]
    public void Create_Dataset_From_Csv_With_Valid_Mashd_Returns_2_Variable_Declarations()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var variableDeclarationCount = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .Count();
        
        Assert.Equal(2, variableDeclarationCount);
    }
    
    [Fact]
    public void Create_Dataset_From_Csv_With_Valid_Mashd_Returns_Dataset_VariableDeclarationNode()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .FirstOrDefault(x => x.DeclaredType == SymbolType.Dataset);
        
        Assert.NotNull(key);
    }
    
    [Fact]
    public void Create_Dataset_From_Csv_With_Valid_Mashd_Returns_DatasetValue()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .First(x => x.DeclaredType == SymbolType.Dataset);

        var value = interpreter.Values[key] as DatasetValue;
        
        Assert.NotNull(value);
    }
    
    [Fact]
    public void Create_Dataset_From_Csv_With_Valid_Mashd_Returns_DatasetValue_With_Loaded_Data()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .First(x => x.DeclaredType == SymbolType.Dataset);

        var value = interpreter.Values[key] as DatasetValue;

        Assert.NotNull(value);
        
        var data = value.Data.ToArray();
        
        Assert.NotEmpty(data);
        Assert.Equal(10, data.Length);
    }
    
    [Fact]
    public void Create_Dataset_From_Csv_With_Invalid_Mashd_Throws_ParseException()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithInvalidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);

        Assert.Throws<ParseException>(() => TestPipeline.Run(content));
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Valid_Mashd_Returns_2_Variable_Declarations()
    {
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var variableDeclarationCount = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .Count();
        
        Assert.Equal(2, variableDeclarationCount);
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Valid_Mashd_Returns_Dataset_VariableDeclarationNode()
    {
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .FirstOrDefault(x => x.DeclaredType == SymbolType.Dataset);
        
        Assert.NotNull(key);
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Valid_Mashd_Returns_DatasetValue()
    {
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .First(x => x.DeclaredType == SymbolType.Dataset);

        var value = interpreter.Values[key] as DatasetValue;
        
        Assert.NotNull(value);
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Valid_Mashd_Returns_DatasetValue_With_Loaded_Data()
    {
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        var (interpreter, _) = TestPipeline.Run(content);
        
        var key = interpreter.Values.Keys
            .OfType<VariableDeclarationNode>()
            .First(x => x.DeclaredType == SymbolType.Dataset);

        var value = interpreter.Values[key] as DatasetValue;

        Assert.NotNull(value);
        
        var data = value.Data.ToArray();
        
        Assert.NotEmpty(data);
        Assert.Equal(10, data.Length);
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Invalid_Mashd_Throws_ParseException()
    {
        var datasetFilePath = _mashdFixture.GenerateMashdFileWithInvalidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        
        Assert.Throws<ParseException>(() => TestPipeline.Run(content));
    }
}