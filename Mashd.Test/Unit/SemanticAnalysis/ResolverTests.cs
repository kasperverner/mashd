using System.Collections.Generic;
using System.Linq;
using Xunit;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.SemanticAnalysis;
using Mashd.Backend.Errors;
using Mashd.Frontend;

namespace TestProject1.Unit.SemanticAnalysis
{
    public class ResolverTests
    {
        // Helper that runs the resolver over a ProgramNode
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
                line: 1, column: 1, text: "<test>",
                1
            );
            resolver.VisitProgramNode(prog);
            return reporter;
        }

        [Fact]
        public void GlobalVariable_ThenUse_IsResolved()
        {
            var decl = new VariableDeclarationNode(
                type: SymbolType.Integer, identifier: "x",
                expression: new LiteralNode(10L, 1, 5, "10", SymbolType.Integer, 1),
                line: 1, column: 1, text: "Integer x = 10;",
                1
            );
            var use = new ExpressionStatementNode(
                expression: new IdentifierNode("x", 2, 1, "x", 1),
                line: 2, column: 1, text: "x;", 1
            );

            var rep = ResolveGlobally(
                defs: Enumerable.Empty<DefinitionNode>(),
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
                new LiteralNode(0L, 1, 5, "0", SymbolType.Integer, 1),
                line: 1, column: 1, text: "Integer a = 0;", 1
            );
            var assign = new AssignmentNode(
                identifier: "a",
                expression: new LiteralNode(5L, 2, 3, "5", SymbolType.Integer, 1),
                line: 2, column: 1, text: "a = 5;", 1
            );

            var rep = ResolveGlobally(
                defs: Enumerable.Empty<DefinitionNode>(),
                stmts: new StatementNode[] { decl, assign }
            );

            Assert.False(rep.HasErrors(ErrorType.NameResolution));
            Assert.Same(decl, assign.Definition);
        }

        [Fact]
        public void UndefinedIdentifier_ReportsError()
        {
            var stmt = new ExpressionStatementNode(
                expression: new IdentifierNode("y", 1, 1, "y", 1),
                line: 1, column: 1, text: "y;", 1
            );

            var rep = ResolveGlobally(
                defs: Enumerable.Empty<DefinitionNode>(),
                stmts: new[] { stmt }
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

            var declOuter = new VariableDeclarationNode(
                SymbolType.Integer, "x",
                new LiteralNode(1L, 1, 5, "1", SymbolType.Integer, level: 1),
                line: 1, column: 1, text: "Integer x = 1;", level: 1
            );
            var globalProg = new ProgramNode(
                imports: new List<ImportNode>(),
                definitions: new List<DefinitionNode>(),
                statements: new List<StatementNode> { declOuter },
                line: 1, column: 1, text: "<root>", level: 1
            );
            resolver.VisitProgramNode(globalProg);

            var declInner = new VariableDeclarationNode(
                SymbolType.Integer, "x",
                new LiteralNode(2L, 2, 5, "2", SymbolType.Integer, level: 2),
                line: 2, column: 3, text: "Integer x = 2;", level: 2
            );
            var useInner = new ExpressionStatementNode(
                new IdentifierNode("x", 3, 5, "x", level: 2),
                line: 3, column: 3, text: "x;", level: 2
            );
            var block = new BlockNode(
                statements: new List<StatementNode> { declInner, useInner },
                line: 2, column: 1, text: "{ Integer x = 2; x; }", level: 2
            );
            resolver.VisitBlockNode(block);

            var useOuter = new ExpressionStatementNode(
                new IdentifierNode("x", 5, 1, "x", level: 1),
                line: 5, column: 1, text: "x;", level: 1
            );
            resolver.VisitExpressionStatementNode(useOuter);

            Assert.False(reporter.HasErrors(ErrorType.NameResolution));

            Assert.Same(declInner, ((IdentifierNode)useInner.Expression).Definition);

            Assert.Same(declOuter, ((IdentifierNode)useOuter.Expression).Definition);
        }


        [Fact]
        public void FunctionCall_KnownFunction_BindsDefinition()
        {
            var fn = new FunctionDefinitionNode(
                "foo", SymbolType.Integer,
                new FormalParameterListNode(new List<FormalParameterNode>(), 1, 10, "()", 1),
                new BlockNode(new List<StatementNode>(), 1, 15, "{}", 1),
                1, 1, "function foo() {}", 1
            );
            var call = new FunctionCallNode("foo", new List<ExpressionNode>(), 2, 1, "foo()", 1);
            var stmt = new ExpressionStatementNode(call, 2, 1, "foo();", 1);

            var rep = ResolveGlobally(
                defs: new[] { fn },
                stmts: new[] { stmt }
            );

            Assert.False(rep.HasErrors(ErrorType.NameResolution));
            Assert.Same(fn, call.Definition);
        }

        [Fact]
        public void FunctionCall_UnknownFunction_ReportsError()
        {
            var call = new FunctionCallNode("bar", new List<ExpressionNode>(), 2, 1, "bar()", 1);
            var stmt = new ExpressionStatementNode(call, 2, 1, "bar();", 1);

            var rep = ResolveGlobally(
                defs: Enumerable.Empty<DefinitionNode>(),
                stmts: new[] { stmt }
            );

            Assert.True(rep.HasErrors(ErrorType.NameResolution));
            var err = rep.Errors.Single(e => e.Type == ErrorType.NameResolution);
            Assert.Contains("Undefined function 'bar'", err.Message);
        }
    }
}