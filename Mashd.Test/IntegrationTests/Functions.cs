using Mashd.Frontend;
using ET = Mashd.Frontend.ErrorType;

namespace Mashd.Test.IntegrationTests;

public class Functions
{
    [Theory]
    // zero-arg integer
    [InlineData(
        @"Integer foo() { 
           return 10; 
         } 
         Integer result = foo();",
        10L
    )]
    // one-arg integer
    [InlineData(
        @"Integer addOne(Integer x) { 
           return x + 1; 
         } 
         Integer result = addOne(41);",
        42L
    )]
    // two-arg integer
    [InlineData(
        @"Integer add(Integer a, Integer b) { 
           return a + b; 
         } 
         Integer result = add(7,5);",
        12L
    )]
    // three-arg integer
    [InlineData(
        @"Integer mul3(Integer a, Integer b, Integer c) { 
           return a * b * c; 
         } 
         Integer result = mul3(2,3,4);",
        24L
    )]
    public void IntegerFunction_VariousArities_ReturnsExpected(string src, long expected)
    {
        var (interp, ast) = TestPipeline.Run(src);
        long actual = TestPipeline.GetInteger(interp, ast, "result");
        Assert.Equal(expected, actual);
    }

    [Theory]
    // one-arg decimal
    [InlineData(
        @"Decimal half(Decimal x) { 
           return x / 2.0; 
         } 
         Decimal result = half(9.0);",
        4.5
    )]
    // mixed-type parameters (Decimal + Integer)
    // [InlineData(
    //   @"Decimal blend(Decimal a, Integer b) {        
    //        return Integer.parse(a) + b;                                    // Needs parsing implemented
    //      } 
    //      Decimal result = blend(2.5, 3);",
    //   5.5
    // )]
    public void DecimalFunction_ReturnsExpected(string src, double expected)
    {
        var (interp, ast) = TestPipeline.Run(src);
        double actual = TestPipeline.GetDecimal(interp, ast, "result");
        Assert.Equal(expected, actual, precision: 10);
    }

    [Theory]
    // zero-arg text
    [InlineData(
        @"Text hello() { 
           return ""hi""; 
         } 
         Text result = hello();",
        "hi"
    )]
    // one-arg text
    [InlineData(
        @"Text echo(Text s) { 
           return s; 
         } 
         Text result = echo(""bye"");",
        "bye"
    )]
    // two-arg text concatenation
    [InlineData(
        @"Text join(Text a, Text b) { 
           return a + b; 
         } 
         Text result = join(""a"", ""b"");",
        "ab"
    )]
    public void TextFunction_ReturnsExpected(string src, string expected)
    {
        var (interp, ast) = TestPipeline.Run(src);
        string actual = TestPipeline.GetText(interp, ast, "result");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecursiveFactorial_Works()
    {
        var src = @"
            Integer fact(Integer n) {
                if (n <= 1) { return 1; }
                return n * fact(n - 1);
            }
            Integer result = fact(5);
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(120, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void RecursiveFibonacci_Works()
    {
        var src = @"
            Integer fib(Integer n) {
                if (n <= 1) { return n; }
                return fib(n-1) + fib(n-2);
            }
            Integer result0 = fib(0);
            Integer result1 = fib(1);
            Integer result5 = fib(5);
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(0, TestPipeline.GetInteger(interp, ast, "result0"));
        Assert.Equal(1, TestPipeline.GetInteger(interp, ast, "result1"));
        Assert.Equal(5, TestPipeline.GetInteger(interp, ast, "result5"));
    }

    [Fact]
    public void SingleLevelNestedCall()
    {
        var src = @"
            // foo returns 5
            Integer foo() {
                return 5;
            }
            // bar returns foo() + 3  ⇒ 8
            Integer bar() {
                return foo() + 3;
            }
            // result should be bar()
            Integer result = bar();
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(8, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void TwoLevelNestedCall()
    {
        var src = @"
            Integer f1() { return 2; }
            Integer f2() { return f1() * 3; }    // 2*3 = 6
            Integer f3() { return f2() + 4; }    // 6+4 = 10
            Integer result = f3();
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(10, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void SumOfMultipleFunctionCalls()
    {
        var src = @"
            Integer a() { return 1; }
            Integer b() { return 2; }
            Integer c() { return 3; }
            // call a()+b()+c() in one expression => 6
            Integer result = a() + b() + c();
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(6, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Theory]
    [InlineData(1, 2, 3, 6)]
    [InlineData(5, 7, 9, 21)]
    public void ParameterizedNestedCalls(int x, int y, int z, int expected)
    {
        // functions that take parameters and call each other
        var src = $@"
            Integer f1(Integer n) {{ return n; }}
            Integer f2(Integer n) {{ return f1(n) + {y}; }}
            Integer f3(Integer n) {{ return f2(n) + {z}; }}
            Integer result = f3({x});
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(expected, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Theory]
    [InlineData(3, 4)]
    [InlineData(10, 11)]
    public void ArgumentIsFunctionCall(int input, int expected)
    {
        var src = $@"
            Integer f1(Integer n) {{
                return n;
            }}
            Integer f2(Integer m) {{
                return m + 1;
            }}
            Integer result = f2(f1({input}));
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(expected, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void DeeplyNestedCallInArgument()
    {
        var src = @"
            Integer one() { return 1; }
            Integer two(Integer x) { return x + 1; }
            Integer three(Integer y) { return y + 1; }

            Integer result = three(two(one()));
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(3, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void FunctionWithBranchedPathReturn()
    {
        var src = @"
            Integer foo() {
                if (true) {
                    return 1;
                } else {
                    return 2;
                }
            }

            Integer result = foo();
        ";
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(1, TestPipeline.GetInteger(interp, ast, "result"));
    }

    [Fact]
    public void FunctionWitoutBranchedPathReturn()
    {
        var src = @"
            Integer foo() {
                if (true) {
                    return 1;
                } else {
                    Integer x = 2;
                }
            }

            Integer result = foo();
        ";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("without returning on some paths", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void NoReturnAtAll()
    {
        string src = @"
                // No return anywhere
                Integer foo() {
                  Integer x = 10;
                }
                Integer result = foo();
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("without returning on some paths", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void IfWithoutElse()
    {
        string src = @"
                // return only in the 'then' branch, no else
                Integer foo() {
                  if (true) {
                    return 1;
                  }
                  // falls through here with no return
                }
                Integer result = foo();
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("without returning on some paths", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void NestedIfChain_MissingFinalReturn()
    {
        string src = @"
                Integer foo() {
                  if (true) {
                    if (false) {
                      return 42;
                    } else {
                      return 43;
                    }
                  }
                  // outer else falls through without return
                }
                Integer result = foo();
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("without returning on some paths", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void ReturnAfterSomeStatements_ButMissingAtEnd()
    {
        string src = @"
                Integer foo() {
                  return 5;
                  // unreachable code is ignored, but if we comment out above return, there is no return
                }
                // Let's simulate by removing that return:
                Integer bar() {
                  Integer x = 1;
                  // no return at all
                }
                Integer result = bar();
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("without returning on some paths", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void FunctionCall_UndefinedFunction()
    {
        string src = "Integer x = foo(1,2);";
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined function 'foo'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    private const string FnDef = @"
            // A simple function taking (Integer, Text) and returning Text
            Text foo(Integer a, Text b) {
                return b;
            }
        ";


    [Theory]
    [InlineData("foo(1)", "expects 2 args")]
    [InlineData("foo(1, \"hello\", \"x\")", "expects 2 args")]
    [InlineData("foo(\"notInt\", \"hello\")", "Argument 1 has type Text, expected Integer")]
    [InlineData("foo(123, 456)", "Argument 2 has type Integer, expected Text")]
    public void FunctionCall_ParameterErrors(string callExpr, string expectedMessage)
    {
        string src = $@"
                {FnDef}
                Text result = {callExpr};
            ";

        // Act & Assert:
        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );

        Assert.Equal(ET.TypeCheck, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains(expectedMessage, System.StringComparison.OrdinalIgnoreCase)
        );
    }
    [Fact]
    public void FunctionReturnsSchemaType()
    {
        var src = @"
            Schema mk() {
              Schema s = {
                foo: { type: Integer, name: ""foo_col"" }
              };
              
              return s;
            }
            Schema result = mk();
        ";
        var (interp, ast) = TestPipeline.Run(src);

        // should be a SchemaValue
        var schemaVal = TestPipeline.GetSchema(interp, ast, "result");
        Assert.NotNull(schemaVal);
    }

// These two tests fail because we load dataset data on creation and the source is not valid.
//     [Fact]
//     public void FunctionReturnsDatasetType()
//     {
//         var src = @"
//             Schema s = { foo: { type: Integer, name: ""foo_col"" } };
//
//             Dataset mkDs() {
//               Dataset d = {
//                 adapter: ""csv"",
//                 schema: s,
//                 source: ""in-memory://foo,1""
//               };
//
//               return d;
//             }
//
//             Dataset result = mkDs();
//         ";
//         var (interp, ast) = TestPipeline.Run(src);
//
//         // should be a DatasetValue
//         var dsVal = TestPipeline.GetDataset(interp, ast, "result");
//         Assert.NotNull(dsVal);
//     }

//     [Fact]
//     public void FunctionReturnsMashdType()
//     {
//         var src = @"
//             Schema s = { foo: { type: Integer, name: ""foo_col"" } };
//
//             Dataset d1 = {
//               adapter: ""csv"",
//               schema: s,
//               source: ""in-memory://foo,1""
//             };
//             Dataset d2 = {
//               adapter: ""csv"",
//               schema: s,
//               source: ""in-memory://foo,2""
//             };
//
//             Mashd mkM() {
//               return d1 & d2;
//             }
//
//             Mashd result = mkM();
//         ";
//         var (interp, ast) = TestPipeline.Run(src);
//
//         // should be a MashdValue
//         var mashdVal = TestPipeline.GetMashd(interp, ast, "result");
//         Assert.NotNull(mashdVal);
//     }

    
}