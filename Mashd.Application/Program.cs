namespace Mashd.Application;

public static class Program
{
    static void Main(string[] args)
    {
        string input = File.ReadAllText("code.mashd");
        MashdInterpreter interpreter = new MashdInterpreter(input);
        
        // Parsing stage
        
        // Lexical Analysis
        interpreter.Lex();
        // Syntactic Analysis
        interpreter.Parse();
        // Ast Construction
        interpreter.BuildAst();
        
        // Import Handling
        interpreter.HandleImports();
        
        // Semantic Analysis stage
        
        // Name Resolution
        interpreter.Resolve();
        // Type Checking
        interpreter.TypeCheck();
        
        // Execution stage
        
        // Interpretation
        interpreter.Interpret();
    }
}