namespace TestProject1.Integration;

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



}