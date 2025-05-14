using System.Collections.Generic;
using Xunit;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.SemanticAnalysis;
using Mashd.Backend.Errors;
using Mashd.Frontend;


namespace TestProject1.Unit.SemanticAnalysis;

public class ResolverTests
{
    // Helper:
    private ErrorReporter ResolveGlobally(
        IEnumerable<DefinitionNode> defs,
        IEnumerable<StatementNode> stmts)
    {
        var reporter = new ErrorReporter();
        var resolver = new Resolver(reporter);
        var prog = new ProgramNode(
            imports: new List<ImportNode>(),
            definitions: defs.ToList(),
            statements: stmts.ToList(),
            line: 1,
            column: 1,
            text: "<test>"
        );
        resolver.VisitProgramNode(prog);
        return reporter;
    }

    [Fact]
    public void GlobalVariable_ThenUse_IsResolved()
    {
        var decl = new VariableDeclarationNode(
            type: SymbolType.Integer, identifier: "x",
            expression: new LiteralNode(10L, 1, 5, "10", SymbolType.Integer),
            hasInitialization: true,
            line: 1, column: 1, text: "Integer x=10;"
        );
        var use = new ExpressionStatementNode(
            expression: new IdentifierNode("x", 2, 1, "x"),
            line: 2, column: 1, text: "x;"
        );

        var rep = ResolveGlobally(
            defs: Array.Empty<DefinitionNode>(),
            stmts: new StatementNode[] { decl, use }
        );

        Assert.False(rep.HasErrors(ErrorType.NameResolution));
        Assert.Same(decl, ((IdentifierNode)use.Expression).Definition);
    }

    [Fact]
    public void Assignment_AfterGlobalDecl_BindsDefinition()
    {
        var decl = new VariableDeclarationNode(
            SymbolType.Integer, "a",
            new LiteralNode(0L, 1, 5, "0", SymbolType.Integer),
            hasInitialization: true,
            line: 1, column: 1, text: "Integer a=0;"
        );
        var assign = new AssignmentNode(
            identifier: "a",
            expression: new LiteralNode(5L, 2, 3, "5", SymbolType.Integer),
            line: 2, column: 1, text: "a=5;"
        );

        var rep = ResolveGlobally(
            defs: Array.Empty<DefinitionNode>(),
            stmts: new StatementNode[] { decl, assign }
        );

        Assert.False(rep.HasErrors(ErrorType.NameResolution));
        Assert.Same(decl, assign.Definition);
    }

    [Fact]
    public void UndefinedIdentifier_ReportsError()
    {
        var use = new ExpressionStatementNode(
            expression: new IdentifierNode("y", 1, 1, "y"),
            line: 1, column: 1, text: "y;"
        );

        var rep = ResolveGlobally(
            defs: Array.Empty<DefinitionNode>(),
            stmts: new[] { use }
        );

        Assert.True(rep.HasErrors(ErrorType.NameResolution));
        var err = rep.Errors.Single(e => e.Type == ErrorType.NameResolution);
        Assert.Contains("Undefined symbol 'y'", err.Message);
    }


    [Fact]
    public void Block_Shadowing_Works()
    {
        var reporter = new ErrorReporter();
        var resolver = new Resolver(reporter);

        var outer = new VariableDeclarationNode(
            SymbolType.Integer, "x",
            new LiteralNode(1L, line: 1, column: 5, text: "1", parsedType: SymbolType.Integer),
            hasInitialization: true,
            line: 1, column: 1, text: "Integer x = 1;"
        );
        var programWithOuter = new ProgramNode(
            imports: new List<ImportNode>(),
            definitions: new List<DefinitionNode>(),
            statements: new List<StatementNode> { outer },
            line: 1, column: 1, text: "<root>"
        );
        resolver.VisitProgramNode(programWithOuter);
        // now 'x' is in global scope

        var innerDecl = new VariableDeclarationNode(
            SymbolType.Integer, "x",
            new LiteralNode(2L, line: 2, column: 5, text: "2", parsedType: SymbolType.Integer),
            hasInitialization: true,
            line: 2, column: 3, text: "Integer x = 2;"
        );
        var useInner = new ExpressionStatementNode(
            expression: new IdentifierNode("x", line: 3, column: 5, text: "x"),
            line: 3, column: 3, text: "x;"
        );
        var block = new BlockNode(
            statements: new List<StatementNode> { innerDecl, useInner },
            line: 2, column: 1, text: "{ Integer x = 2; x; }"
        );
        resolver.VisitBlockNode(block);

        var useOuter = new ExpressionStatementNode(
            expression: new IdentifierNode("x", line: 5, column: 1, text: "x"),
            line: 5, column: 1, text: "x;"
        );
        resolver.VisitExpressionStatementNode(useOuter);

        Assert.False(reporter.HasErrors(ErrorType.NameResolution));

        // inner 'x' bound to innerDecl
        Assert.Same(innerDecl, ((IdentifierNode)useInner.Expression).Definition);

        // outer 'x' bound to outer
        Assert.Same(outer, ((IdentifierNode)useOuter.Expression).Definition);
    }
    
    [Fact]
    public void FunctionCall_KnownFunction_BindsDefinition()
    {
        var fn = new FunctionDefinitionNode(
            "foo", SymbolType.Integer,
            new FormalParameterListNode(new List<FormalParameterNode>(), 1,10, "()"),
            new BlockNode(new List<StatementNode>(), 1,15, "{}"),
            1,1, "function foo() {}"
        );
        var call = new FunctionCallNode("foo", new List<ExpressionNode>(), 2,1, "foo()");
        var stmt = new ExpressionStatementNode(call, 2,1, "foo();");

        var rep = ResolveGlobally(
            defs:  new[] { fn },
            stmts: new[] { stmt }
        );

        Assert.False(rep.HasErrors(ErrorType.NameResolution));
        Assert.Same(fn, call.Definition);
    }

    [Fact]
    public void FunctionCall_UnknownFunction_ReportsError()
    {
        var call = new FunctionCallNode("bar", new List<ExpressionNode>(), 2,1, "bar()");
        var stmt = new ExpressionStatementNode(call, 2,1, "bar();");

        var rep = ResolveGlobally(
            defs:  Array.Empty<DefinitionNode>(),
            stmts: new[] { stmt }
        );

        Assert.True(rep.HasErrors(ErrorType.NameResolution));
        var err = rep.Errors.Single(e => e.Type == ErrorType.NameResolution);
        Assert.Contains("Undefined function 'bar'", err.Message);
    }
    
            [Fact]
        public void Dataset_WithValidSchema_ResolvesSchema()
        {
            // schema S { }
            var schema = new SchemaDefinitionNode(
                identifier: "S",
                objectNode: new SchemaObjectNode(
                    fields: new Dictionary<string, SchemaField>(), 
                    line:1, column:1, text:"schema S { }"
                ),
                line:1, column:1, text:"schema S { }"
            );

            // dataset D { schema: S }
            var props = new Dictionary<string, DatasetObjectNode.DatasetProperty>{
                ["schema"] = new DatasetObjectNode.DatasetProperty(
                    key:   "schema",
                    value: new IdentifierNode("S",2,12,"S")
                )
            };
            var ds = new DatasetDefinitionNode(
                identifier: "D",
                objectNode: new DatasetObjectNode(
                    line:2, column:1, text:"dataset D { schema: S }", 
                    properties: props
                ),
                line:2, column:1, text:"dataset D { schema: S }"
            );

            var rep = ResolveGlobally(
                defs:  new DefinitionNode[]{ schema, ds },
                stmts: Array.Empty<StatementNode>()
            );

            Assert.False(rep.HasErrors(ErrorType.NameResolution));
            //Assert.Same(schema, ds.ResolvedSchema);
        }

        [Fact]
        public void Dataset_WithInvalidSchema_ReportsError()
        {
            var schema = new SchemaDefinitionNode(
                "S",
                new SchemaObjectNode(new Dictionary<string, SchemaField>(), 1,1,"schema S { }"),
                1,1,"schema S { }"
            );

            var props = new Dictionary<string, DatasetObjectNode.DatasetProperty>{
                ["schema"] = new DatasetObjectNode.DatasetProperty(
                    "schema",
                    new IdentifierNode("X",2,12,"X")
                )
            };
            var ds = new DatasetDefinitionNode(
                "D",
                new DatasetObjectNode(2,1,"dataset D { schema: X }", props),
                2,1,"dataset D { schema: X }"
            );

            var rep = ResolveGlobally(
                defs:  new DefinitionNode[]{ schema, ds },
                stmts: new StatementNode[0]
            );

            Assert.True(rep.HasErrors(ErrorType.NameResolution));
            var err = rep.Errors.Single(e => e.Type == ErrorType.NameResolution);
            Assert.Contains("Undefined schema 'X'", err.Message);
        }
    
}