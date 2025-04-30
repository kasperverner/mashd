namespace TestProject1.Integration;

public class Arithmetics
{
    [Theory]
    // Integer addition
    [InlineData("Integer", "5 + 7", 12L)]
    [InlineData("Integer", "4294967295 + 1", 4294967296L)]
    [InlineData("Integer", "9223372036854775807 + 1", -9223372036854775808L)]
    [InlineData("Integer", "-37 + 22", -15L)]
    [InlineData("Integer", "-37 + -14", -51L)]
    // Integer subtraction
    [InlineData("Integer", "10 - 4", 6L)]
    [InlineData("Integer", "5 - 7", -2L)]
    [InlineData("Integer", "0 - 1", -1L)]
    [InlineData("Integer", "0 - 0", 0L)]
    [InlineData("Integer", "0 - -1", 1L)]
    [InlineData("Integer", "-9223372036854775807 - 2", 9223372036854775807L)]
    // Decimal addition
    [InlineData("Decimal", "1.5 + 2.25", 3.75)]
    [InlineData("Decimal", "5.0 + 7.0", 12.0)]
    [InlineData("Decimal", "4294967295.0 + 1.0", 4294967296.0)]
    [InlineData("Decimal", "9223372036854775807.0 + 1.0", 9223372036854775808.0)]
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
    [InlineData("Integer", "1 / 0", 0L)] // Division by zero should be handled
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
    [InlineData("Decimal", "1.0 / 0.0", 0.0)] // Division by zero should be handled
    [InlineData("Decimal", "1.0 / 1.0", 1.0)]
    // Integer modulo
    [InlineData("Integer", "10 % 3", 1L)]
    [InlineData("Integer", "15 % 4", 3L)]
    [InlineData("Integer", "4 % 4", 0L)]
    [InlineData("Integer", "0 % 1", 0L)]
    [InlineData("Integer", "1 % 0", 0L)] // Modulo by zero should be handled
    // Text concatenation
    [InlineData("Text", "\"Hello\" + \" World\"", "Hello World")]
    [InlineData("Text", "\"Mashd\" + \" Interpreter\"", "Mashd Interpreter")]
    [InlineData("Text", "\"\" + \"\"", "")]
    [InlineData("Text", "\"\" + \"Hello\"", "Hello")]
    // Integer negation
    [InlineData("Integer", "-5", -5L)]
    [InlineData("Integer", "-0", 0L)]
    [InlineData("Integer", "-9223372036854775807", 9223372036854775807L)]
    // Decimal negation
    [InlineData("Decimal", "-5.0", -5.0)]
    [InlineData("Decimal", "-0.0", 0.0)]
    [InlineData("Decimal", "-9223372036854775807.0", 9223372036854775807.0)]
    // Boolean not
    // [InlineData("Boolean", "!true", false)] // Cannot parse boolean "true" or "false" literal currently
    // [InlineData("Boolean", "!false", true)]
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
}