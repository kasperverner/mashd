using Mashd.Frontend;
using ET = Mashd.Frontend.ErrorType;

namespace TestProject1.Integration;

public class Scoping
{
    [Fact]
    public void Shadowing_InIfElseBranches()
    {
        string src = @"
                Integer x = 5;
                if (x > 0) {
                    Integer x = 10;
                    Integer result1 = x * 2;   // should use inner x = 10 => 20
                } else {
                    Integer result2 = x * 3;   // not taken
                }
                // after the branch, outer x (5) is back in scope
                Integer final = x + 1;          // 5 + 1 => 6
            ";

        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(6, TestPipeline.GetInteger(interp, ast, "final"));
    }

    [Fact]
    public void OuterVariable_VisibleInsideFunction()
    {
        string src = @"
                Integer g = 42;
                Integer foo() {
                    // should see the global 'g'
                    return g;
                }
                Integer result = foo();  // 42
            ";

        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(42, TestPipeline.GetInteger(interp, ast, "result"));
    }
    
    [Fact]
    public void ForwardReference_InSameScopeFails()
    {
        // x is used before being declared
        string src = @"
                Integer y = x;
                Integer x = 1;
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined symbol 'x'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void BlockVariable_NotVisibleOutside_If()
    {
        // 'tmp' is declared inside the if and should not be visible after
        string src = @"
                if (true) {
                    Integer tmp = 123;
                }
                Integer after = tmp;  // tmp is undefined here
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined symbol 'tmp'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void BlockVariable_NotVisibleOutside_Function()
    {
        // 'inner' is local to foo() and not visible at global level
        string src = @"
                Integer foo() { 
                    Integer inner = 7; 
                    return inner; 
                }
                Integer result = foo();   // ok
                Integer bad = inner;      // 'inner' is undefined here
            ";

        var ex = Assert.Throws<FrontendException>(() =>
            TestPipeline.RunFull(src)
        );
        Assert.Equal(ET.NameResolution, ex.Phase);
        Assert.Contains(ex.Errors, e =>
            e.Message.Contains("Undefined symbol 'inner'", System.StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Shadowing_AllowsInnerToHideOuter()
    {
        // but you *can* shadow: this is legal
        string src = @"
                    Integer x = 1;
                    Integer temp;
                    if (true) {
                      Integer x = 2;
                      temp = x;          // assign into an outer `temp`
                    }
                    Integer outside = x;

                    Integer inside = temp;
            ";

        // Run through without exception
        var (interp, ast) = TestPipeline.Run(src);
        Assert.Equal(2, TestPipeline.GetInteger(interp, ast, "inside"));
        Assert.Equal(1, TestPipeline.GetInteger(interp, ast, "outside"));
    }
    
}