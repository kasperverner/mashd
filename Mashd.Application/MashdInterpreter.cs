using Antlr4.Runtime;
using Mashd.Frontend.SemanticAnalysis;
using Mashd.Backend;
using Mashd.Backend.Errors;
using Mashd.Frontend;
using Mashd.Frontend.AST;

namespace Mashd.Application;


public class MashdInterpreter(string input, int level = 0)
{
    private readonly ErrorReporter _errorReporter = new();
    private readonly HashSet<string> _processedFiles = new();

    private readonly string _input = input;
    private readonly int _level = level;
    
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
        AstBuilder astBuilder = new AstBuilder(_errorReporter, _level);
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

    public MashdInterpreter HandleImports()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before handling imports.");
        }

        foreach (var importNode in Ast.Imports)
        {
            var importPath = importNode.Path;
            if (!File.Exists(importPath))
            {
                Console.Error.WriteLine($"Import file not found: {importPath}");
                continue;
            }

            if (!_processedFiles.Add(importPath))
            {
                // Skip already processed files to prevent circular dependencies
                continue;
            }

            var importedContent = File.ReadAllText(importPath);
            var importedInterpreter = new MashdInterpreter(importedContent, _level - 1);

            // Pass the current processed files to the imported interpreter to avoid reprocessing
            importedInterpreter._processedFiles.UnionWith(_processedFiles);

            importedInterpreter.Lex();
            importedInterpreter.Parse();
            importedInterpreter.BuildAst();

            if (importedInterpreter.Ast == null)
            {
                Console.Error.WriteLine($"Failed to build AST for imported file: {importPath}");
                continue;
            }

            // Gather the imports from the imported interpreter and add them to the current interpreter
            _processedFiles.UnionWith(importedInterpreter._processedFiles);

            // Merge the AST of the imported file into the current AST
            Ast.Merge(importedInterpreter.Ast);
        }

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
    }

    private void CheckErrors(ErrorType phase)
    {
        if (_errorReporter.HasErrors(phase))
        {
            var errors = _errorReporter.Errors;
            throw new FrontendException(phase, errors);
        }
    }

    private void ReportException(RuntimeException  ex)
    {
        throw ex;
    }
}