using Antlr4.Runtime;
using Mashd.Frontend.SemanticAnalysis;
using Antlr4.Runtime.Tree;
using Mashd.Backend;
using Mashd.Backend.Errors;
using Mashd.Frontend;
using Mashd.Frontend.AST;

namespace Mashd.Application;


public class MashdInterpreter
{
    private readonly ErrorReporter errorReporter = new();
    
    private readonly HashSet<string> _processedFiles = new();
    
    private string Input;
    private CommonTokenStream? Tokens { get; set; }
    private IParseTree? Tree { get; set; }
    public ProgramNode? Ast { get; private set; }
    
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

    public void Run()
    {
        Lex();
        Parse();
        BuildAst();
        HandleImports();
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
        Ast = (ProgramNode) astBuilder.Visit(Tree);

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
        
        CheckErrors(ErrorType.NameResolution);
        return this;
    }
    public MashdInterpreter TypeCheck()
    {
        if (Ast == null)
        {
            throw new InvalidOperationException("AstBuilder must be run before TypeChecker.");
        }
        TypeChecker typeChecker = new TypeChecker(errorReporter);
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

            if (_processedFiles.Contains(importPath))
            {
                // Skip already processed files to prevent circular dependencies
                continue;
            }

            _processedFiles.Add(importPath);
            
            var importedContent = File.ReadAllText(importPath);
            var importedInterpreter = new MashdInterpreter(importedContent);
            
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
        Value result = null;
        try
        {
            Interpreter interpreter = new Interpreter();
            result = interpreter.Evaluate((ProgramNode)Ast);
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
        Console.WriteLine("Last line result:");
        Console.WriteLine(result.ToString());
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