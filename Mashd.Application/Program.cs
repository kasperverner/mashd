using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Mashd.Frontend.AST;

namespace Mashd.Application;

public static class Program
{
    static void Main(string[] args)
    {
            
        // Read input file and run the lexer and parser.
        string input = File.ReadAllText("code.mashd");
        AntlrInputStream inputStream = new AntlrInputStream(input);
        MashdLexer lexer = new MashdLexer(inputStream);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        MashdParser parser = new MashdParser(tokens);
            
        // Create and print the ANTLR parse tree.
        IParseTree tree = parser.program();
        Console.WriteLine("Parse Tree:");
        Console.WriteLine(tree.ToStringTree(parser));
            
        // Create own AST from the ANTLR parse tree and print it.
        AstBuilder astBuilder = new AstBuilder();
        AstNode ast = astBuilder.Visit(tree);
        Console.WriteLine("\nAST:");
        // PrintAst(ast);
        
    }
    
}