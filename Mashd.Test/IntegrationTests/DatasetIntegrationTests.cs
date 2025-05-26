using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;
using Mashd.Test.Fixtures;

namespace Mashd.Test.IntegrationTests;

public class DatasetIntegrationTests(IntegrationCsvFixture csv, IntegrationPostgreSqlFixture db, IntegrationMashdFixture mashd)
    : IClassFixture<IntegrationCsvFixture>, IClassFixture<IntegrationPostgreSqlFixture>, IClassFixture<IntegrationMashdFixture>
{
    private readonly IntegrationCsvFixture _csv = csv;
    private readonly IntegrationPostgreSqlFixture _db = db;
    private readonly IntegrationMashdFixture _mashd = mashd;

    [Fact]
    public void Create_Dataset_From_Csv_With_Valid_Mashd_Returns_2_Variable_Declarations()
    {
        var filePath = _csv.TemporaryFilePath;
        var datasetFilePath = _mashd.GenerateMashdFileWithValidCsvDataset(filePath);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidCsvDataset(filePath);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidCsvDataset(filePath);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidCsvDataset(filePath);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithInvalidCsvDataset(filePath);

        var content = File.ReadAllText(datasetFilePath);

        Assert.Throws<Exception>(() => TestPipeline.Run(content));
    }
    
    [Fact]
    public void Create_Dataset_From_Db_With_Valid_Mashd_Returns_2_Variable_Declarations()
    {
        var datasetFilePath = _mashd.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithValidPostgreSqlDataset(_db.ConnectionString);

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
        var datasetFilePath = _mashd.GenerateMashdFileWithInvalidPostgreSqlDataset(_db.ConnectionString);

        var content = File.ReadAllText(datasetFilePath);
        
        Assert.Throws<Exception>(() => TestPipeline.Run(content));
    }
}