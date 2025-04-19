using Antlr4.Runtime;
using Mashd.Frontend.AST;

namespace Mashd.Frontend;

public class ErrorReporter
{
    private readonly List<Error> _errors = new List<Error>();

    public IReadOnlyList<Error> Errors
    {
        get { return _errors; }
    }

    public bool HasAnyErrors
    {
        get { return _errors.Any(); }
    }

    public bool HasErrors(ErrorType phase)
    {
        return _errors.Any(e => e.Type == phase);
    }

    public Reporter Report { get; }

    public ErrorReporter()
    {
        Report = new Reporter(this);
    }

    internal void Add(ErrorType type, int line, int column, string message, string sourceText)
    {
        Error error = new Error(type, line, column, message, sourceText);
        _errors.Add(error);
    }

    internal void Add(ErrorType type, AstNode node, string message)
    {
        Add(type, node.Line, node.Column, message, node.Text);
    }

    public void Clear()
    {
        _errors.Clear();
    }

    public class Reporter

    {
        private readonly ErrorReporter _parent;

        public Reporter(ErrorReporter parent)
        {
            _parent = parent;
        }

        public void Lexical(int line, int column, string sourceText, string message)
        {
            _parent.Add(ErrorType.Lexical, line, column, message, sourceText);
        }

        public void Syntactic(int line, int column, string sourceText, string message)
        {
            _parent.Add(ErrorType.Syntactic, line, column, message, sourceText);
        }

        public void AstBuilder(AstNode node, string message)
        {
            _parent.Add(ErrorType.AstBuilder, node, message);
        }

        public void AstBuilder(int line, int column, string sourceText, string message)
        {
            _parent.Add(ErrorType.AstBuilder, line, column, message, sourceText);
        }

        public void AstBuilder(ParserRuleContext ctx, string message)
        {
            int line = ctx.Start.Line;
            int column = ctx.Start.Column;
            string text = ctx.GetText();
            _parent.Add(ErrorType.AstBuilder, line, column, message, text);
        }

        public void NameResolution(AstNode node, string message)
        {
            _parent.Add(ErrorType.NameResolution, node, message);
        }

        public void TypeCheck(AstNode node, string message)
        {
            _parent.Add(ErrorType.TypeCheck, node, message);
        }

        public void Interpretation(AstNode node, string message)
        {
            _parent.Add(ErrorType.Interpretation, node, message);
        }
    }
}