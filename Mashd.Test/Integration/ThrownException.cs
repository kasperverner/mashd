using Mashd.Frontend;
using ET = Mashd.Frontend.ErrorType;

namespace Mashd.Test.Integration;

public class ThrownException
{
    [Fact]
    public void DuplicateKey_InObjectExpression()
    {
        string src = @"Dataset d = { a: 1, a: 2 };";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.AstBuilder, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Duplicate key", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Assignment_UndefinedSymbol()
    {
        string src = "Integer x = y;";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined symbol", System.StringComparison.OrdinalIgnoreCase)
        );
    }
    

    [Fact]
    public void Identifier_UndefinedSymbol()
    {
        string src = "Boolean b = bar;";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined symbol 'bar'", System.StringComparison.OrdinalIgnoreCase)
        );
    }


    [Fact]
    public void DatasetObject_UndefinedSchemaIdentifier()
    {
        string src = @"
                Dataset d = { schema: UnknownSchema };
            ";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined schema 'UnknownSchema'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DatasetObject_SchemaNotIdentifier()
    {
        string src = @"
                Dataset d = { schema: ""notAnId"" };
            ";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Schema property must be an identifier", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("Text t = 123;", "assign")]
    [InlineData("Integer x = 0; x = \"foo\";", "assign")]
    [InlineData("Decimal d = 5.5; d = 2;", "assign")]
    public void VariableDeclaration_Or_Assignment_Mismatch(string src, string keyword)
    {
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void ReturnStatement_TypeMismatch()
    {
        string src = @"
                Integer foo() {
                    return ""bar"";
                }
            ";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("does not match expected", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void IfCondition_NotBoolean()
    {
        string src = "if (123) { }";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("must be Boolean", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void TernaryCondition_NotBoolean()
    {
        // Use an explicit declaration: pick a type for the result
        string src = "Integer x = 1 ? 2 : 3;";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Ternary condition must be Boolean", System.StringComparison.OrdinalIgnoreCase)
        );
    }


    [Fact]
    public void TernaryArms_MismatchedTypes()
    {
        string src = "Integer x = true ? 1 : 2.5;";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("arms must have same type", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("-\"hello\"", "requires numeric")]
    [InlineData("!123",        "requires Boolean")]
    public void UnaryOperator_WrongOperand(string expr, string keyword)
    {
        string src = $"Integer x = {expr};";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("1 + true",     "requires both sides")]
    [InlineData("true + false", "requires numeric")]
    [InlineData("5.5 % 2",      "requires integer")]
    [InlineData("\"a\" < \"b\"","requires numeric")]
    [InlineData("true && 0",    "requires both sides of same type")]
    public void BinaryOperator_Mismatches(string expr, string keyword)
    {
        // Pick a result type that makes sense (e.g. Boolean for comparisons)
        string src = $"Boolean b = {expr};";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)
        );
    }


    [Fact]
    public void PropertyAccess_WrongTargetType()
    {
        string src = "Integer x = 123.foo;";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("requires Schema or Dataset", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void MethodChain_InvalidMethodForType()
    {
        string src = "Text t = \"hello\".join();";
        var ex = Assert.Throws<FrontendException>(() => TestPipeline.RunFull(src));
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("is not valid on expression of type", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DatasetDefinition_MissingRequiredProperty()
    {
        string src = @"Dataset d = { source: ""x"", adapter: ""csv"" };";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Required property", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DatasetDefinition_UnknownProperty()
    {
        string src = @"
                        Schema testSchema = {
                          ID: {
                            type: Integer,
                            name: ""user_id""
                            }
                        };

                        Dataset d = { adapter: ""csv"", source: ""x"", schema: testSchema, foo: 1 };";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Unknown property 'foo'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void DatasetDefinition_DuplicateProperty()
    {
        string src = @"Dataset d = { adapter: ""csv"", adapter: ""csv"", source: ""x"", schema: s };";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.AstBuilder, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Duplicate key in object expression", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("adapter: 5, source: \"x\", schema: testSchema", "must be Text")]
    [InlineData("adapter: \"xml\", source: \"x\", schema: testSchema", "Unsupported adapter")]
    [InlineData("adapter: \"csv\", source: 42, schema: testSchema", "must be Text")]
    [InlineData("adapter: \"csv\", source: \"x\", schema: testSchema, skip: \"no\"", "must be an Integer")]
    public void DatasetDefinition_PropertyTypeErrors(string datasetProps, string expectedKeyword)
    {
        // Arrange
        string src = $@"
                Schema testSchema = {{
                  ID: {{
                    type: Integer,
                    name: ""user_id""
                  }}
                }};
                
                Dataset d = {{
                  {datasetProps}
                }};
            ";

        // Act & Assert
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);

        Assert.Contains(ex.Errors, e =>
            e.Message.Contains(expectedKeyword, System.StringComparison.OrdinalIgnoreCase)
        );
    }
}