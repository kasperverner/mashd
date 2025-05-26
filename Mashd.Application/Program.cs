using Mashd.Backend.Errors;
using Mashd.Frontend;

namespace Mashd.Application;

public static class Program
{
    static int Main(string[] args)
    {
        try
        {
            string input = File.ReadAllText("code.mashd");
            var interpreter = new MashdInterpreter(input);

            // --- front‐end stages ---
            interpreter.Lex();
            interpreter.Parse();
            interpreter.BuildAst();
            interpreter.HandleImports();
            interpreter.Resolve();
            interpreter.TypeCheck();

            // --- execution ---
            interpreter.Interpret();

            return 0;
        } catch (FrontendException fe)
        {
            // dump all the front‐end errors and exit nonzero
            Console.Error.WriteLine($"Found {fe.Errors.Count} error(s) during {fe.Phase}:");
            foreach (var e in fe.Errors)
                Console.Error.WriteLine(e);
            return 1;
        }
        catch (RuntimeException re)
        {
            // a runtime error during Interpret()
            Console.Error.WriteLine("Runtime error:");
            Console.Error.WriteLine(re.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unexpected error:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}