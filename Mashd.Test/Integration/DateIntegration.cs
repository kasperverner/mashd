using Antlr4.Runtime;
using Mashd.Backend;
using Mashd.Frontend;
using Mashd.Frontend.AST;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Test.Integration;

public class DateIntegration
{
    private AstNode BuildAst(string input)
    {
        var lexer = new MashdLexer(new AntlrInputStream(input));
        var parser = new MashdParser(new CommonTokenStream(lexer));
        var astBuilder = new AstBuilder(new ErrorReporter(), 0);
        return astBuilder.VisitProgram(parser.program());
    }
    
    private void RunPipeline(string input)
    {
        var errorReporter = new ErrorReporter();
        var ast = BuildAst(input);

        // Resolving
        var resolver = new Resolver(errorReporter);
        resolver.Resolve(ast);

        // Type-checking
        var typeChecker = new TypeChecker(errorReporter);
        ast.Accept(typeChecker);

        // Interpretation
        var interpreter = new Interpreter();
        interpreter.Evaluate(ast);

        if (errorReporter.HasErrors(ErrorType.Interpretation))
        {
            throw new Exception("Pipeline errors: " + string.Join(", ", errorReporter.Errors));
        }
    }
    
    [Theory]
    [InlineData("Date date = Date.parse(\"2023-10-01\");")]
    [InlineData("Date date = Date.parse(\"01-10-2023\", \"dd-MM-yyyy\");")]
    [InlineData("Date date = Date.parse(\"10/01/2023\", \"MM/dd/yyyy\");")]
    [InlineData("Date date = Date.parse(\"2023-10-01\", \"yyyy-MM-dd\");")]
    public void DateParse_ValidInputs_ShouldPassThroughPipeline(string input)
    {
        RunPipeline(input);
    }

    [Theory]
    [InlineData("Date date = Date.parse(\"2023-13-01\");", typeof(FormatException))]
    [InlineData("Date date = Date.parse(\"01-32-2023\", \"MM-dd-yyyy\");", typeof(FormatException))]
    [InlineData("Date date = Date.parse(\"invalid-date\");", typeof(FormatException))]
    [InlineData("Date date = Date.parse(\"2023-10-01\", \"invalid-format\");", typeof(FormatException))]
    public void DateParse_InvalidInputs_ShouldThrowException(string input, Type expectedException)
    {
        Assert.Throws(expectedException, () => RunPipeline(input));
    }
}
