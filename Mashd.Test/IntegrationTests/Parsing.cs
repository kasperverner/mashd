using Mashd.Frontend;

namespace Mashd.Test.IntegrationTests;

public class Parsing
{
    [Theory]
    // // Integer.parse
    [InlineData("Integer.parse(\"42\")", "Integer", 42L)]
    [InlineData("Integer.parse(42.9)", "Integer", 42L)]
    [InlineData("Integer.parse(123)", "Integer", 123L)]
    // Decimal.parse
    [InlineData("Decimal.parse(\"3.14\")", "Decimal", 3.14)]
    [InlineData("Decimal.parse(5)", "Decimal", 5.0)]
    [InlineData("Decimal.parse(2.718)", "Decimal", 2.718)]
    // Text.parse from any literal
    [InlineData("Text.parse(\"foo\")", "Text", "foo")]
    [InlineData("Text.parse(123)", "Text", "123")]
    [InlineData("Text.parse(3.5)", "Text", "3.5")]
    [InlineData("Text.parse(true)", "Text", "True")]
    // Boolean.parse
    [InlineData("Boolean.parse(\"true\")", "Boolean", true)]
    [InlineData("Boolean.parse(false)", "Boolean", false)]
    public void StaticParse_ReturnsExpectedValue(string expr, string resultType, object expectedRaw)
    {
        // Arrange
        var src = $"{resultType} result = {expr};";

        // Act
        var (interp, ast) = TestPipeline.Run(src);

        // Assert
        switch (resultType)
        {
            case "Integer":
                var i = TestPipeline.GetInteger(interp, ast, "result");
                Assert.Equal((long)expectedRaw, i);
                break;

            case "Decimal":
                var d = TestPipeline.GetDecimal(interp, ast, "result");
                Assert.Equal((double)expectedRaw, d, precision: 10);
                break;

            case "Text":
                var s = TestPipeline.GetText(interp, ast, "result");
                Assert.Equal((string)expectedRaw, s);
                break;

            case "Boolean":
                var b = TestPipeline.GetBoolean(interp, ast, "result");
                Assert.Equal((bool)expectedRaw, b);
                break;

            default:
                throw new InvalidOperationException("Unexpected type");
        }
    }

    [Theory]
    // Integer.parse(Text foo)
    [InlineData("Integer", "Text", "\"123\"", 123L)]
    // Integer.parse(Decimal foo)
    [InlineData("Integer", "Decimal", "42.9", 42L)]
    // Decimal.parse(Integer foo)
    [InlineData("Decimal", "Integer", "7", 7.0)]
    // Decimal.parse(Decimal foo)
    [InlineData("Decimal", "Decimal", "2.5", 2.5)]
    // Text.parse(Boolean foo)
    [InlineData("Text", "Boolean", "false", "False")]
    // Boolean.parse(Text foo)
    [InlineData("Boolean", "Text", "\"true\"", true)]
    public void StaticParse_VariableArgument_Works(string resultType, string fooType, string fooLiteral,
        object expectedRaw)
    {
        // Arrange: declare foo of fooType
        var src = $@"
            {fooType} foo = {fooLiteral};
            {resultType} result = {resultType}.parse(foo);
        ";

        // Act
        var (interp, ast) = TestPipeline.Run(src);

        // Assert
        switch (resultType)
        {
            case "Integer":
                var i = TestPipeline.GetInteger(interp, ast, "result");
                Assert.Equal((long)expectedRaw, i);
                break;

            case "Decimal":
                var d = TestPipeline.GetDecimal(interp, ast, "result");
                Assert.Equal((double)expectedRaw, d, precision: 10);
                break;

            case "Text":
                var s = TestPipeline.GetText(interp, ast, "result");
                Assert.Equal((string)expectedRaw, s);
                break;

            case "Boolean":
                var b = TestPipeline.GetBoolean(interp, ast, "result");
                Assert.Equal((bool)expectedRaw, b);
                break;

            default:
                throw new InvalidOperationException("Unexpected type");
        }
    }


    [Theory]
    [InlineData("Integer", "1")]
    [InlineData("Decimal", "1.23")]
    [InlineData("Text", "\"hi\"")]
    [InlineData("Boolean", "false")]
    public void Parse_OnValue_ThrowsTypeCheck(string fooType, string fooLiteral)
    {
        // Arrange: one foo in scope
        var src = $@"
            {fooType} foo = {fooLiteral};
            Integer result = foo.parse(""123"");
        ";

        // Act & Assert
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ErrorType.TypeCheck, ex.Phase);
        Assert.Contains("parse() must be invoked on a type literal", ex.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }


    [Theory]
    [InlineData("Integer.parse()")]
    [InlineData("Decimal.parse()")]
    [InlineData("Text.parse()")]
    [InlineData("Integer.parse(\"1\", \"2\")")]
    [InlineData("Boolean.parse(true, false)")]
    public void Parse_WrongArity_ThrowsTypeCheck(string callExpr)
    {
        var src = $@"
            {callExpr};  // standalone expr
        ";

        // We need a variable declaration so the program is syntactically valid
        src = $"Integer dummy = 0;\n{src}\nInteger result = Integer.parse(\"0\");";

        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ErrorType.TypeCheck, ex.Phase);
        Assert.Contains("Cannot parse ,", ex.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    // Integer.parse only accepts Text, Integer, Decimal
    [InlineData("Integer.parse(true)")]
    [InlineData("Integer.parse(Date.parse(\"2025-05-13\"))")]

    // Decimal.parse only accepts Text, Integer, Decimal
    [InlineData("Decimal.parse(false)")]
    [InlineData("Decimal.parse(Date.parse(\"2025-05-13\"))")]

    // Boolean.parse only accepts Text or Boolean
    [InlineData("Boolean.parse(123)")]
    [InlineData("Boolean.parse(Date.parse(\"2025-05-13\"))")]

    // Date.parse only accepts Text
    [InlineData("Date.parse(123)")]
    [InlineData("Date.parse(true)")]
    public void Parse_BadArgType_ThrowsTypeCheck(string callExpr)
    {
        var src = $@"
            {callExpr};  // standalone expr
        ";

        src = $"Text dummy = \"x\";\n{src}\nInteger result = Integer.parse(\"0\");";

        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ErrorType.TypeCheck, ex.Phase);
        Assert.Contains("Cannot parse", ex.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }
}