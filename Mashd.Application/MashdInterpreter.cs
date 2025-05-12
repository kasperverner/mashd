using Antlr4.Runtime;
using Mashd.Frontend.SemanticAnalysis;
using Antlr4.Runtime.Tree;
using Mashd.Backend;
using Mashd.Backend.Errors;
using Mashd.Frontend;
using Mashd.Frontend.AST;

namespace Mashd.Application;


public class MashdInterpreter(string input)
{
    private readonly ErrorReporter _errorReporter = new();

    private readonly string _input = input;
    private CommonTokenStream? Tokens { get; set; }
    private MashdParser.ProgramContext? Context { get; set; }
    public ProgramNode? Ast { get; private set; }
    
    public MashdInterpreter Lex()
    {
        AntlrInputStream inputStream = new AntlrInputStream(_input);
        MashdLexer lexer = new MashdLexer(inputStream);
        
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new AntlrLexerErrorListener(_errorReporter));
        
        Tokens = new CommonTokenStream(lexer);
        
        CheckErrors(ErrorType.Lexical);

        return this;
    }

    // TODO: Do we maintain this method as it is not used in the current implementation?
    public void Run()
    {
        Lex();
        Parse();
        BuildAst();
        Resolve();
        TypeCheck();
        Interpret();
    }
    
    public MashdInterpreter Parse()
    {
        if (Tokens is null)
        {
            throw new InvalidOperationException("Lexer must be run before Parser.");
        }
        MashdParser parser = new MashdParser(Tokens);
        
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new AntlrParserErrorListener(_errorReporter));
        
        Context = parser.program();
        
        CheckErrors(ErrorType.Syntactic);

        Console.WriteLine(Context.ToStringTree(parser));
        return this;
    }
    
    public MashdInterpreter BuildAst()
    {
        if (Context == null)
        {
            throw new InvalidOperationException("Parser must be run before AstBuilder.");
        }
        AstBuilder astBuilder = new AstBuilder(_errorReporter);
        Ast = (ProgramNode) astBuilder.Visit(Context);

        CheckErrors(ErrorType.AstBuilder);
        return this;
    }
    public MashdInterpreter Resolve()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before Resolver.");
        }
        Resolver resolver = new Resolver(_errorReporter);
        resolver.Resolve((ProgramNode)Ast);
        
        CheckErrors(ErrorType.NameResolution);
        return this;
    }
    public MashdInterpreter TypeCheck()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before TypeChecker.");
        }
        TypeChecker typeChecker = new TypeChecker(_errorReporter);
        typeChecker.Check((ProgramNode)Ast);
        
        CheckErrors(ErrorType.TypeCheck);
        
        return this;
    }
    
    public void Interpret()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before Interpreter.");
        }
        
        try
        {
            var interpreter = new Interpreter();
            var result = interpreter.Evaluate(Ast);
            
            Console.WriteLine("Last line result:");
            Console.WriteLine(result.ToString());
        }
        catch (RuntimeException ex)
        {
            ReportException(ex);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Unexpected error during interpretation:");
            Console.Error.WriteLine(e.Message);
            Environment.Exit(1);
        }
    }
    
    private void CheckErrors(ErrorType phase)
    {
        if (_errorReporter.HasErrors(phase))
        {
            Console.Error.WriteLine("Errors:");
            foreach (var e in _errorReporter.Errors)
            {
                Console.Error.WriteLine(e);
            }
            Environment.Exit(1);
        }
    }
    
    private void ReportException(RuntimeException  ex)
    {
        Console.Error.WriteLine("Error during interpretation:");
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1);
    }
}