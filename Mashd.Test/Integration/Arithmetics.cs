using Mashd.Backend.Errors;

namespace TestProject1.Integration;

public class Arithmetics
{
    [Theory]
    // Integer addition
    [InlineData("Integer", "5 + 7", 12L)]
    [InlineData("Integer", "-37 + 22", -15L)]
    [InlineData("Integer", "-37 + -14", -51L)]
    // Integer subtraction
    [InlineData("Integer", "10 - 4", 6L)]
    [InlineData("Integer", "5 - 7", -2L)]
    [InlineData("Integer", "0 - 1", -1L)]
    [InlineData("Integer", "0 - 0", 0L)]
    [InlineData("Integer", "0 - -1", 1L)]
    // Decimal addition
    [InlineData("Decimal", "1.5 + 2.25", 3.75)]
    [InlineData("Decimal", "5.0 + 7.0", 12.0)]
    // Decimal subtraction
    [InlineData("Decimal", "5.0 - 0.75", 4.25)]
    [InlineData("Decimal", "5.0 - 7.0", -2.0)]
    [InlineData("Decimal", "0.0 - 1.0", -1.0)]
    [InlineData("Decimal", "0.0 - 0.0", 0.0)]
    [InlineData("Decimal", "0.0 - -1.0", 1.0)]
    // Integer multiplication
    [InlineData("Integer", "3 * 4", 12L)]
    [InlineData("Integer", "10 * 2", 20L)]
    [InlineData("Integer", "0 * 1", 0L)]
    [InlineData("Integer", "1 * 0", 0L)]
    [InlineData("Integer", "1 * 1", 1L)]
    [InlineData("Integer", "1 * -1", -1L)]
    [InlineData("Integer", "-1 * -1", 1L)]
    // Integer division
    [InlineData("Integer", "20 / 5", 4L)]
    [InlineData("Integer", "10 / 2", 5L)]
    [InlineData("Integer", "0 / 1", 0L)]
    [InlineData("Integer", "1 / 1", 1L)]
    // Decimal multiplication
    [InlineData("Decimal", "2.5 * 4.0", 10.0)]
    [InlineData("Decimal", "10.0 * 2.5", 25.0)]
    [InlineData("Decimal", "0.0 * 1.0", 0.0)]
    [InlineData("Decimal", "1.0 * 0.0", 0.0)]
    [InlineData("Decimal", "1.0 * 1.0", 1.0)]
    [InlineData("Decimal", "1.0 * -1.0", -1.0)]
    [InlineData("Decimal", "-1.0 * -1.0", 1.0)]
    // Decimal division
    [InlineData("Decimal", "10.0 / 2.5", 4.0)]
    [InlineData("Decimal", "20.0 / 5.0", 4.0)]
    [InlineData("Decimal", "0.0 / 1.0", 0.0)]
    [InlineData("Decimal", "1.0 / 1.0", 1.0)]
    // Integer modulo
    [InlineData("Integer", "10 % 3", 1L)]
    [InlineData("Integer", "15 % 4", 3L)]
    [InlineData("Integer", "4 % 4", 0L)]
    [InlineData("Integer", "0 % 1", 0L)]
    // Text concatenation
    [InlineData("Text", "\"Hello\" + \" World\"", "Hello World")]
    [InlineData("Text", "\"Mashd\" + \" Interpreter\"", "Mashd Interpreter")]
    [InlineData("Text", "\"\" + \"\"", "")]
    [InlineData("Text", "\"\" + \"Hello\"", "Hello")]
    // Integer negation
    [InlineData("Integer", "-5", -5L)]
    [InlineData("Integer", "-0", 0L)]
    // Decimal negation
    [InlineData("Decimal", "-5.0", -5.0)]
    [InlineData("Decimal", "-0.0", 0.0)]
    // // Boolean not
    [InlineData("Boolean", "!true", false)]
    [InlineData("Boolean", "!false", true)]
    public void Arithmetic_OnPrimitives(string type, string expr, object expected)
    {
        // Arrange:
        string source = $"{type} test = {expr};";

        // Act:
        var (interpreter, ast) = TestPipeline.Run(source);

        // Assert:
        if (type == "Integer")
        {
            long actual = TestPipeline.GetInteger(interpreter, ast, "test");
            Assert.Equal((long)expected, actual);
        }
        else if (type == "Decimal")
        {
            double actual = TestPipeline.GetDecimal(interpreter, ast, "test");
            Assert.Equal((double)expected, actual, precision: 10);
        }

        else if (type == "Text")
        {
            string actual = TestPipeline.GetText(interpreter, ast, "test");
            Assert.Equal((string)expected, actual);
        }
    }
    
     [Theory]
     [InlineData("Integer", "1 / 0")]
     [InlineData("Decimal", "1.0 / 0.0")]
     [InlineData("Integer", "1 % 0")]
     public void DivisionByZero_ThrowsException(string type, string expr)
     {
         // Arrange
         string source = $"{type} test = {expr};";
     
         // Act & Assert
         var exception = Assert.Throws<DivisionByZeroException>(() =>
         {
             var (interpreter, ast) = TestPipeline.Run(source);
             if (type == "Integer")
                 TestPipeline.GetInteger(interpreter, ast, "test");
             else if (type == "Decimal")
                 TestPipeline.GetDecimal(interpreter, ast, "test");
         });
     }

     [Theory]
     // simple additive/multiplicative mix
     [InlineData("Integer", "10 - 7 * 3", -11L)]
     [InlineData("Integer", "(10 - 7) * 3", 9L)]
     [InlineData("Integer", "4 * (10 - 6) / 2", 8L)]
     [InlineData("Integer", "21 / (2 + 5) * 7", 21L)] // Fails because * has higher precedence than /, needs fix
     
     // deeper nesting
     [InlineData("Integer", "(2 + 3) * (4 + 1)", 25L)]
     [InlineData("Integer", "((2 + 3) * 4) + 1", 21L)]
     [InlineData("Integer", "(2 + (3 * 4)) + 1", 15L)]
     
     // modulo binds with * and /
     [InlineData("Integer", "20 % 6 * 2", 4L)] // Fails because * has higher precedence than %, needs fix
     [InlineData("Integer", "20 % (6 * 2)", 8L)] // 6*2=12 → 20%12
     
     // mix all four at one level
     [InlineData("Integer", "2 + 3 * 4 - 5 / 5", 13L)] // 2 + (3*4) − (5/5)=2 → 2+12−1
     
     // left associativity of same-level ops
     [InlineData("Integer", "100 / 5 / 2", 10L)] // (100/5)=20 /2=10
     [InlineData("Integer", "100 - 20 - 5", 75L)] // (100−20)=80 −5=75
     
     // unary binds tightest
     [InlineData("Integer", "-2 * 3 + 5", -1L)] // (-2*3)=−6 +5
     [InlineData("Integer", "-(2 * (3 + 2))", -10L)]
     
     // decimal tests
     [InlineData("Decimal", "5.0 + 2.5 * 2.0", 10.0)]
     [InlineData("Decimal", "(5.0 + 2.5) * 2.0", 15.0)]
     [InlineData("Decimal", "10.0 / 4.0 / 2.0", 1.25)]
     [InlineData("Decimal", "10.0 / (4.0 / 2.0)", 5.0)]
     
     // more nested
     [InlineData("Integer", "((1+2)*(3+4))/(2+5)", 3L)]
     public void OrderOfPrecedenceOperations(string type, string expr, object expected)
     {
         // Arrange:
         string source = $"{type} test = {expr};";
     
         // Act:
         var (interpreter, ast) = TestPipeline.Run(source);
     
         // Assert:
         if (type == "Integer")
         {
             long actual = TestPipeline.GetInteger(interpreter, ast, "test");
             Assert.Equal((long)expected, actual);
         }
         else if (type == "Decimal")
         {
             double actual = TestPipeline.GetDecimal(interpreter, ast, "test");
             Assert.Equal((double)expected, actual, precision: 10);
         }
     
         else if (type == "Text")
         {
             string actual = TestPipeline.GetText(interpreter, ast, "test");
             Assert.Equal((string)expected, actual);
         }
     }
}