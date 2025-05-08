using Mashd.Frontend.AST;

namespace Mashd.Frontend.SemanticAnalysis;

public class SymbolTable
{
    private readonly Dictionary<string, IDeclaration> _symbols = new Dictionary<string, IDeclaration>();
    
    public SymbolTable Parent { get; }
    private readonly ErrorReporter errorReporter;

    public SymbolTable(ErrorReporter errorReporter, SymbolTable parent = null)
    {
        this.errorReporter = errorReporter;
        Parent = parent;
    }
    public void Add(string name, IDeclaration declaration)
    {
        if (_symbols.ContainsKey(name))
        {
            errorReporter.Report.NameResolution((AstNode) declaration,$"Symbol '{name}' is already defined in the current scope.");
        }
        _symbols[name] = declaration;
    }

    public bool ContainsInCurrentScope(string name)
    {
        return _symbols.ContainsKey(name);
    }

    public bool TryLookup(string name, out IDeclaration declaration)
    {
        if (_symbols.TryGetValue(name, out declaration))
            return true;
        if (Parent != null)
            return Parent.TryLookup(name, out declaration);
        declaration = null;
        return false;
    }

    public IDeclaration Lookup(string name)
    {
        if (TryLookup(name, out var declaration))
        {
            return declaration;
        }
        errorReporter.Report.NameResolution((AstNode) declaration, $"Symbol '{name}' not found in any enclosing scope.");
        return null;
    }
    
    public bool IsGlobalScope => Parent == null;

}