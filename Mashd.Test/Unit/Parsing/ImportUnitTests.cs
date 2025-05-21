using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Mashd.Backend;

namespace Mashd.Test.Unit.Parsing;

public class ImportUnitTests
{
    private static IParseTree ParseProgram(string input)
    {
        var inputStream = new AntlrInputStream(input);
        var lexer = new MashdLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new MashdParser(tokenStream);
        
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());
            
        return parser.program();
    }

    [Fact]
    public void CanParseImportStatement()
    {
        // Arrange
        string input = "import \"another_file.mashed\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithAbsolutePath()
    {
        // Arrange
        string input = "import \"/absolute/path/to/file.mashed\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithRelativePath()
    {
        // Arrange
        string input = "import \"../relative/path/to/file.mashed\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithWindowsPath()
    {
        // Arrange
        string input = "import \"C:\\\\Path\\\\To\\\\File.mashed\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithDifferentFileExtensions()
    {
            // Arrange
        string[] inputs = new[]
        {
            "import \"file.mashd\";",
            "import \"file.mshd\";",
            "import \"file.md\";",
            "import \"file.txt\";",
            "import \"file.json\";"
        };
        
        foreach (var input in inputs)
        {
            // Act
            var result = ParseProgram(input);
            
            // Assert
            Assert.NotNull(result);
        }
    }
    
    [Fact]
    public void CanParseImportWithNoFileExtension()
    {
        // Arrange
        string input = "import \"filename\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithSpacesInPath()
    {
        // Arrange
        string input = "import \"path with spaces/file name.mashd\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseImportWithSpecialCharactersInPath()
    {
        // Arrange
        string input = "import \"path-with_special$chars/file-name_123.mashd\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Theory]
    [InlineData("import \"valid_file.mashd\";", true)]
    [InlineData("import valid_file.mashd;", false)]  
    [InlineData("import \"valid_file.mashd\"", false)]
    [InlineData("import \"\";", true)] 
    [InlineData("import;", false)]  
    public void ValidatesImportStatementSyntax(string input, bool shouldBeValid)
    {
        try
        {
            // Act
            var result = ParseProgram(input);
            
            // Assert
            if (shouldBeValid)
            {
                Assert.NotNull(result);
            }
            else
            {
                Assert.True(false, "Parser did not throw an exception for invalid syntax");
            }
        }
        catch (Exception)
        {
            Assert.False(shouldBeValid);
        }
    }
    
    [Fact]
    public void CanParseMultipleImportStatements()
    {
        // Arrange
        string input = "import \"file1.mashd\";\nimport \"file2.mashd\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanParseMixedImportAndStatements()
    {
        // Arrange
        string input = "import \"file1.mashd\";\nInteger x = 42;";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void CanHandlePathsWithQuoteCharacters()
    {
        // Arrange
        string input = "import \"file_with_\\\"quotes\\\".mashd\";";
        
        // Act
        var result = ParseProgram(input);
        
        // Assert
        Assert.NotNull(result);
    }
}