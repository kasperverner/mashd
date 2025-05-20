using System.Linq;
using Xunit;
using Mashd.Frontend;
using Mashd.Frontend.AST;
using Mashd.Frontend.SemanticAnalysis;

namespace TestProject1.Unit.SemanticAnalysis;

public class SymbolTableTests
{
    // Helpers
    private class TestDeclaration : AstNode, IDeclaration
    {
        public string Identifier { get; }
        public SymbolType DeclaredType { get; }

        public TestDeclaration(
            string identifier,
            SymbolType declaredType = SymbolType.Unknown,
            int line = 1,
            int column = 1,
            string text = "")
            : base(line, column, text, 1)
        {
            Identifier = identifier;
            DeclaredType = declaredType;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            throw new System.NotSupportedException();
        }
    }

    private static (ErrorReporter reporter, SymbolTable table) SetupReporter()
    {
        var reporter = new ErrorReporter();
        var table = new SymbolTable(reporter);
        return (reporter, table);
    }

    private IDeclaration MakeDecl(string id) => new TestDeclaration(id);

    [Fact]
    public void Add_SingleSymbol_CanBeLookedUp()
    {
        var (_, table) = SetupReporter();
        var decl = MakeDecl("x");

        table.Add("x", decl);

        Assert.True(table.TryLookup("x", out var found));
        Assert.Same(decl, found);
    }

    [Fact]
    public void Add_DuplicateSymbol_RecordsError()
    {
        var (reporter, table) = SetupReporter();
        var first = MakeDecl("x");
        var second = MakeDecl("x");

        table.Add("x", first);
        table.Add("x", second);

        var errors = reporter.Errors;
        Assert.Single(errors);

        var err = errors[0];
        Assert.Equal(ErrorType.NameResolution, err.Type);
        Assert.Equal(second.Line, err.Line);
        Assert.Equal(second.Column, err.Column);
        Assert.Equal("Symbol 'x' is already defined in the current scope.", err.Message);

        Assert.Equal(((AstNode)second).Text, err.SourceText);
    }

    [Fact]
    public void Lookup_SymbolNotFound_ThrowsNullReferenceException()
    {
        var (reporter, table) = SetupReporter();

        var ex = Assert.Throws<NullReferenceException>(() => table.Lookup("missing"));
        Assert.Contains("ErrorReporter.Add", ex.StackTrace);
    }

    [Fact]
    public void TryLookup_FallsBackToParentScope()
    {
        var _reporter = new ErrorReporter();
        var parent = new SymbolTable(_reporter);
        var child = new SymbolTable(_reporter, parent);
        var decl = MakeDecl("foo");

        parent.Add("foo", decl);

        Assert.True(child.TryLookup("foo", out var found));
        Assert.Same(decl, found);
    }

    [Fact]
    public void ContainsInCurrentScope_DoesNotSeeParentSymbols()
    {
        var _reporter = new ErrorReporter();
        var parent = new SymbolTable(_reporter);
        var child = new SymbolTable(_reporter, parent);
        parent.Add("a", MakeDecl("a"));

        Assert.False(child.ContainsInCurrentScope("a"));
    }

    [Fact]
    public void IsGlobalScope_TrueWhenNoParent()
    {
        var table = new SymbolTable(new ErrorReporter());
        Assert.True(table.IsGlobalScope);
    }

    [Fact]
    public void IsGlobalScope_FalseWhenHasParent()
    {
        var parent = new SymbolTable(new ErrorReporter());
        var child = new SymbolTable(new ErrorReporter(), parent);
        Assert.False(child.IsGlobalScope);
    }
}