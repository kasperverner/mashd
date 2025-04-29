using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Mashd.Frontend;
using Mashd.Frontend.AST;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Application;


public class MashdInterpreter
{
    private readonly ErrorReporter errorReporter = new();
    
    private string Input;
    private CommonTokenStream? Tokens { get; set; }
    private IParseTree? Tree { get; set; }
    private AstNode? Ast { get; set; }
    
    private SymbolTable? Symbols { get; set; }
    
    public MashdInterpreter(string input)
    {
        Input = input;
    }
    
    public MashdInterpreter Lex()
    {
        AntlrInputStream inputStream = new AntlrInputStream(Input);
        MashdLexer lexer = new MashdLexer(inputStream);
        
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new AntlrLexerErrorListener(errorReporter));
        
        Tokens = new CommonTokenStream(lexer);
        
        CheckErrors(ErrorType.Lexical);

        return this;
    }
    
    public MashdInterpreter Parse()
    {
        if (Tokens is null)
        {
            throw new InvalidOperationException("Lexer must be run before Parser.");
        }
        MashdParser parser = new MashdParser(Tokens);
        
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new AntlrParserErrorListener(errorReporter));
        
        Tree = parser.program();
        
        CheckErrors(ErrorType.Syntactic);

        Console.WriteLine(Tree.ToStringTree(parser));
        return this;
    }
    
    public MashdInterpreter BuildAst()
    {
        if (Tree == null)
        {
            throw new InvalidOperationException("Parser must be run before AstBuilder.");
        }
        AstBuilder astBuilder = new AstBuilder(errorReporter);
        Ast = astBuilder.Visit(Tree);

        CheckErrors(ErrorType.AstBuilder);
        return this;
    }
    public MashdInterpreter Resolve()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before Resolver.");
        }
        Resolver resolver = new Resolver(errorReporter);
        resolver.Resolve((ProgramNode)Ast);
        
        Symbols = resolver.GlobalScope;
        
        CheckErrors(ErrorType.NameResolution);
        return this;
    }
    public MashdInterpreter TypeCheck()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before TypeChecker.");
        }
        TypeChecker typeChecker = new TypeChecker(errorReporter, Symbols);
        typeChecker.Check((ProgramNode)Ast);
        
        CheckErrors(ErrorType.TypeCheck);
        return this;
    }
    
    private void CheckErrors(ErrorType phase)
    {
        if (errorReporter.HasErrors(phase))
        {
            Console.Error.WriteLine("Errors:");
            foreach (var e in errorReporter.Errors)
            {
                Console.Error.WriteLine(e);
            }
            
            // Exit with error code
            Environment.Exit(1);
        }
    }
}