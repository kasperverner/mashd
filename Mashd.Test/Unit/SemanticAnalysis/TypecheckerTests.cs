using Mashd.Frontend;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Test.Unit.SemanticAnalysis
{
    public class TypeCheckerTests
    {
        // Helpers
        private ErrorReporter CheckNode(AstNode node)
        {
            var reporter = new ErrorReporter();
            var checker = new TypeChecker(reporter);
            checker.Check(node);
            return reporter;
        }

        private ErrorReporter CheckProgram(IEnumerable<DefinitionNode> defs, IEnumerable<StatementNode> stmts)
        {
            var reporter = new ErrorReporter();
            var checker = new TypeChecker(reporter);
            var prog = new ProgramNode(
                imports: new List<ImportNode>(),
                definitions: defs.ToList(),
                statements: stmts.ToList(),
                line: 1, column: 1, text: "<test>", 1
            );
            checker.VisitProgramNode(prog);
            return reporter;
        }

        [Fact]
        public void ReturnOutsideFunction_ReportsError()
        {
            var ret = new ReturnNode(
                expression: new LiteralNode(0L, 1, 1, "0", SymbolType.Integer, level: 1),
                line: 1, column: 1, text: "return 0;", level: 1
            );

            var rep = CheckNode(ret);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Return statement outside of function", rep.Errors[0].Message);
        }

        [Fact]
        public void ReturnTypeMismatch_ReportsError()
        {
            var body = new BlockNode(
                statements: new List<StatementNode>
                {
                    new ReturnNode(
                        expression: new LiteralNode("text", 1, 10, "\"text\"", SymbolType.Text, level: 1),
                        line: 1, column: 1, text: "return \"text\";", level: 1
                    )
                },
                line: 1, column: 1, text: "{ return \"text\"; }", level: 1
            );
            var fn = new FunctionDefinitionNode(
                functionName: "foo",
                returnType: SymbolType.Integer,
                parameterList: new FormalParameterListNode(new List<FormalParameterNode>(), 1, 5, "()", level: 1),
                body: body,
                line: 1, column: 1, text: "function foo() { return \"text\"; }", level: 1
            );

            var rep = CheckProgram(new[] { fn }, Enumerable.Empty<StatementNode>());
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Return type Text does not match expected type Integer", rep.Errors[0].Message);
        }

        [Fact]
        public void MissingReturnPath_ReportsError()
        {
            var body = new BlockNode(
                statements: new List<StatementNode>(),
                line: 1, column: 1, text: "{ }", level: 1
            );
            var fn = new FunctionDefinitionNode(
                functionName: "bar",
                returnType: SymbolType.Decimal,
                parameterList: new FormalParameterListNode(new List<FormalParameterNode>(), 1, 5, "()", level: 1),
                body: body,
                line: 1, column: 1, text: "function bar() { }", level: 1
            );

            var rep = CheckProgram(new[] { fn }, Enumerable.Empty<StatementNode>());
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Function 'bar' may exit without returning on some paths", rep.Errors[0].Message);
        }

        [Fact]
        public void VariableInitialization_TypeMismatch_ReportsError()
        {
            var decl = new VariableDeclarationNode(
                type: SymbolType.Integer,
                identifier: "x",
                expression: new LiteralNode("true", 1, 5, "true", SymbolType.Boolean, level: 1),
                line: 1, column: 1, text: "Integer x = true;", level: 1
            );

            var rep = CheckNode(decl);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Cannot assign Boolean to variable of type Integer", rep.Errors[0].Message);
        }

        [Fact]
        public void BinaryTypeMismatch_ReportsError()
        {
            var left = new LiteralNode(1L, 1, 1, "1", SymbolType.Integer, level: 1);
            var right = new LiteralNode("a", 1, 3, "\"a\"", SymbolType.Text, level: 1);
            var bin = new BinaryNode(left, right, OpType.Add, line: 1, column: 1, text: "1 + \"a\"", level: 1);

            var rep = CheckNode(bin);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Binary operator requires both sides of same type", rep.Errors[0].Message);
        }

        [Fact]
        public void UnaryNegation_NonNumeric_ReportsError()
        {
            var operand = new LiteralNode(true, 1, 2, "true", SymbolType.Boolean, level: 1);
            var unary = new UnaryNode(
                operand: operand,
                unaryOperator: OpType.Negation,
                line: 1, column: 1, text: "-true", level: 1
            );

            var rep = CheckNode(unary);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Negation '-' requires numeric operand", rep.Errors[0].Message);
        }

        [Fact]
        public void IfCondition_NonBoolean_ReportsError()
        {
            var cond = new LiteralNode(123L, 1, 4, "123", SymbolType.Integer, level: 1);
            var ifNode = new IfNode(
                condition: cond,
                thenBlock: new BlockNode(new List<StatementNode>(), 1, 5, "{}", level: 1),
                hasElse: false,
                elseBlock: null,
                line: 1, column: 1, text: "if (123) {}", level: 1
            );

            var rep = CheckNode(ifNode);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("'if' condition must be Boolean", rep.Errors[0].Message);
        }

        [Fact]
        public void Ternary_MismatchedArms_ReportsError()
        {
            var tern = new TernaryNode(
                condition: new LiteralNode(true, 1, 1, "true", SymbolType.Boolean, level: 1),
                trueExpression: new LiteralNode(1L, 1, 7, "1", SymbolType.Integer, level: 1),
                falseExpression: new LiteralNode("no", 1, 10, "\"no\"", SymbolType.Text, level: 1),
                line: 1, column: 1, text: "true ? 1 : \"no\"", level: 1
            );

            var rep = CheckNode(tern);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Ternary arms must have same type", rep.Errors[0].Message);
        }

        [Fact]
        public void DateLiteral_InvalidFormat_ReportsError()
        {
            var dateLit = new LiteralNode("2025-13-01", 1, 1, "\"2025-13-01\"", SymbolType.Date, level: 1);
            var rep = CheckNode(dateLit);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Invalid date format: 2025-13-01. Expected ISO 8601 (yyyy-MM-dd)", rep.Errors[0].Message);
        }

        [Fact]
        public void MethodChain_ParseInvalidArg_ReportsError()
        {
            var typeLit = new TypeLiteralNode(SymbolType.Boolean, 1, 1, "Boolean", SymbolType.Boolean, level: 1);
            var argument = new LiteralNode(123L, 1, 15, "123", SymbolType.Integer, level: 1);
            var chain = new MethodChainExpressionNode(
                left: typeLit,
                methodName: "parse",
                arguments: new List<ExpressionNode> { argument },
                next: null,
                line: 1, column: 1, text: "Boolean.parse(123)", level: 1
            );

            var rep = CheckNode(chain);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Cannot parse , as Boolean", rep.Errors[0].Message);
        }

        [Fact]
        public void FunctionCall_WrongArgCount_ReportsError()
        {
            // function foo(a: Integer, b: Integer) { return 0; }
            var fn = new FunctionDefinitionNode(
                "foo", SymbolType.Integer,
                new FormalParameterListNode(new List<FormalParameterNode>
                {
                    new FormalParameterNode(SymbolType.Integer, "a", 1, 1, "Integer a", 1),
                    new FormalParameterNode(SymbolType.Integer, "b", 1, 1, "Integer b", 1)
                }, 1, 1, "(Integer a, Integer b)", 1),
                new BlockNode(new List<StatementNode>
                {
                    new ReturnNode(new LiteralNode(0L, 1, 1, "0", SymbolType.Integer, 1), 1, 1, "return 0;", 1)
                }, 1, 1, "{}", 1),
                1, 1, "function foo(Integer a, Integer b) { return 0; }", 1
            );

            // call with 1 argument instead of 2
            var call = new FunctionCallNode(
                "foo",
                new List<ExpressionNode>
                {
                    new LiteralNode(1L, 2, 1, "1", SymbolType.Integer, 1)
                },
                2, 1, "foo(1)", 1
            );
            call.Definition = fn;
            var stmt = new ExpressionStatementNode(call, 2, 1, "foo(1);", 1);

            var rep = CheckProgram(new[] { fn }, new[] { stmt });
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("expects 2 args, but got 1 args", rep.Errors[0].Message);
        }


        [Fact]
        public void FunctionCall_ArgTypeMismatch_ReportsError()
        {
            var fn = new FunctionDefinitionNode(
                "bar", SymbolType.Text,
                new FormalParameterListNode(new List<FormalParameterNode>
                {
                    new FormalParameterNode(SymbolType.Text, "x", 1, 1, "Text x", level: 1)
                }, 1, 1, "(Text x)", level: 1),
                new BlockNode(new List<StatementNode>
                {
                    new ReturnNode(new LiteralNode("", 1, 1, "\"\"", SymbolType.Text, level: 1), 1, 1, "return \"\";",
                        level: 1)
                }, 1, 1, "{}", level: 1),
                1, 1, "function bar(Text x) { return \"\"; }", level: 1
            );

            var call = new FunctionCallNode(
                "bar",
                new List<ExpressionNode> { new LiteralNode(123L, 2, 1, "123", SymbolType.Integer, level: 1) },
                2, 1, "bar(123)", level: 1
            );
            call.Definition = fn;
            var stmt = new ExpressionStatementNode(call, 2, 1, "bar(123);", level: 1);

            var rep = CheckProgram(new[] { fn }, new[] { stmt });
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Argument 1 has type Integer, expected Text", rep.Errors[0].Message);
        }

        [Fact]
        public void ValidArithmetic_NoErrors()
        {
            var a = new LiteralNode(2L, 1, 1, "2", SymbolType.Integer, level: 1);
            var b = new LiteralNode(3L, 1, 5, "3", SymbolType.Integer, level: 1);
            var expr = new BinaryNode(a, b, OpType.Multiply, 1, 1, "2 * 3", level: 1);

            var rep = CheckNode(expr);
            Assert.False(rep.HasErrors(ErrorType.TypeCheck));
        }


        [Fact]
        public void MethodChain_ParseOnNonType_ReportsError()
        {
            var chain = new MethodChainExpressionNode(
                left: new LiteralNode("123", 1, 1, "\"123\"", SymbolType.Text, level: 1),
                methodName: "parse",
                arguments: new List<ExpressionNode>(),
                next: null,
                line: 1, column: 1, text: "\"123\".parse()", level: 1
            );

            var rep = CheckNode(chain);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("parse() must be invoked on a type literal", rep.Errors[0].Message);
        }


        [Fact]
        public void Combine_NonDatasetOperands_ReportsError()
        {
            // "foo" +combine "bar"
            var left = new LiteralNode("foo", 1, 1, "\"foo\"", SymbolType.Text, level: 1);
            var right = new LiteralNode("bar", 1, 1, "\"bar\"", SymbolType.Text, level: 1);
            var combine = new BinaryNode(left, right, OpType.Combine, 1, 1, "\"foo\" combine \"bar\"", level: 1);

            var rep = CheckNode(combine);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Combine requires two datasets", rep.Errors[0].Message);
        }

        [Fact]
        public void Combine_OneOperandNotDataset_ReportsError()
        {
            var dsDef = new VariableDeclarationNode(
                type: SymbolType.Dataset,
                identifier: "x",
                expression: null,
                line: 1, column: 1, text: "Dataset x", level: 1
            );
            var id = new IdentifierNode("x", 1, 1, "x", level: 1) { Definition = dsDef };

            var right = new LiteralNode(123L, 1, 5, "123", SymbolType.Integer, level: 1);

            var combine = new BinaryNode(id, right, OpType.Combine, 1, 1, "x combine 123", level: 1);

            var rep = CheckNode(combine);

            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Binary operator requires both sides", rep.Errors[0].Message);
        }

        [Fact]
        public void Combine_TwoDatasets_NoError()
        {
            var dsA = new VariableDeclarationNode(
                SymbolType.Dataset, "a", null, line: 1, column: 1, text: "Dataset a", level: 1
            );
            var dsB = new VariableDeclarationNode(
                SymbolType.Dataset, "b", null, line: 1, column: 1, text: "Dataset b", level: 1
            );

            var idA = new IdentifierNode("a", 1, 1, "a", level: 1) { Definition = dsA };
            var idB = new IdentifierNode("b", 1, 1, "b", level: 1) { Definition = dsB };

            var combine = new BinaryNode(idA, idB, OpType.Combine, 1, 1, "a combine b", level: 1);
            var rep = CheckNode(combine);
            Assert.False(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Equal(SymbolType.Mashd, combine.InferredType);
        }

        [Fact]
        public void PropertyAccess_NonSchemaDataset_ReportsError()
        {
            var lit = new LiteralNode(42L, 1, 1, "42", SymbolType.Integer, level: 1);
            var access = new PropertyAccessExpressionNode(lit, "foo", 1, 1, "42.foo", level: 1);

            var rep = CheckNode(access);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Property access requires Dataset", rep.Errors[0].Message);
        }

        [Fact]
        public void MethodChain_ToTableOnNonDataset_ReportsError()
        {
            var lit = new LiteralNode("notDs", 1, 1, "\"notDs\"", SymbolType.Text, level: 1);
            var chain = new MethodChainExpressionNode(
                left: lit,
                methodName: "toTable",
                arguments: new List<ExpressionNode>(),
                next: null,
                line: 1, column: 1, text: "\"notDs\".toTable()", level: 1
            );

            var rep = CheckNode(chain);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Invalid left-hand side type 'Mashd.Frontend.AST.Expressions.LiteralNode'", rep.Errors[0].Message);
        }
    }
}