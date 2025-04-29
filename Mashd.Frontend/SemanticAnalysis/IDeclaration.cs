using Mashd.Frontend.AST;

namespace Mashd.Frontend.SemanticAnalysis;

public interface IDeclaration
{
    string Identifier { get; }
    SymbolType DeclaredType { get; }
    
    public int Line { get; }
    public int Column { get; }


}