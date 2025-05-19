using System.Collections.Generic;
using Xunit;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;
using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.SemanticAnalysis;
using Mashd.Backend.Errors;
using Mashd.Frontend;

namespace TestProject1.Unit.SemanticAnalysis
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
                definitions: new List<DefinitionNode>(defs),
                statements: new List<StatementNode>(stmts),
                line: 1, column: 1, text: "<test>"
            );
            checker.VisitProgramNode(prog);
            return reporter;
        }

        [Fact]
        public void ReturnOutsideFunction_ReportsError()
        {
            var ret = new ReturnNode(
                expression: new LiteralNode(0L, 1, 1, "0", SymbolType.Integer),
                line: 1, column: 1, text: "return 0;"
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
                        expression: new LiteralNode("text", 1, 10, "\"text\"", SymbolType.Text),
                        line: 1, column: 1, text: "return \"text\";"
                    )
                },
                line: 1, column: 1, text: "{ return \"text\"; }"
            );
            var fn = new FunctionDefinitionNode(
                functionName: "foo",
                returnType: SymbolType.Integer,
                parameterList: new FormalParameterListNode(new List<FormalParameterNode>(), 1, 5, "()"),
                body: body,
                line: 1, column: 1, text: "function foo() { return \"text\"; }"
            );

            var rep = CheckProgram(new[] { fn }, new StatementNode[0]);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Return type Text does not match expected type Integer", rep.Errors[0].Message);
        }

        [Fact]
        public void MissingReturnPath_ReportsError()
        {
            var body = new BlockNode(
                statements: new List<StatementNode>(),
                line: 1, column: 1, text: "{ }"
            );
            var fn = new FunctionDefinitionNode(
                functionName: "bar",
                returnType: SymbolType.Decimal,
                parameterList: new FormalParameterListNode(new List<FormalParameterNode>(), 1, 5, "()"),
                body: body,
                line: 1, column: 1, text: "function bar() { }"
            );

            var rep = CheckProgram(new[] { fn }, new StatementNode[0]);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Function 'bar' may exit without returning on some paths", rep.Errors[0].Message);
        }

        [Fact]
        public void VariableInitialization_TypeMismatch_ReportsError()
        {
            var decl = new VariableDeclarationNode(
                type: SymbolType.Integer,
                identifier: "x",
                expression: new LiteralNode("true", 1, 5, "true", SymbolType.Boolean),
                hasInitialization: true,
                line: 1, column: 1, text: "Integer x = true;"
            );

            var rep = CheckNode(decl);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Cannot assign Boolean to variable of type Integer", rep.Errors[0].Message);
        }

        [Fact]
        public void BinaryTypeMismatch_ReportsError()
        {
            var left = new LiteralNode(1L, 1, 1, "1", SymbolType.Integer);
            var right = new LiteralNode("a", 1, 3, "\"a\"", SymbolType.Text);
            var bin = new BinaryNode(
                left: left,
                op: OpType.Add,
                right: right,
                line: 1, column: 1, text: "1 + \"a\""
            );

            var rep = CheckNode(bin);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Binary operator requires both sides of same type", rep.Errors[0].Message);
        }

        [Fact]
        public void UnaryNegation_NonNumeric_ReportsError()
        {
            var operand = new LiteralNode(true, 1, 2, "true", SymbolType.Boolean);
            var unary = new UnaryNode(
                unaryOperator: OpType.Negation,
                operand: operand,
                line: 1, column: 1, text: "-true"
            );

            var rep = CheckNode(unary);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Negation '-' requires numeric operand", rep.Errors[0].Message);
        }

        [Fact]
        public void IfCondition_NonBoolean_ReportsError()
        {
            var cond = new LiteralNode(123L, 1, 4, "123", SymbolType.Integer);
            var ifNode = new IfNode(
                condition: cond,
                thenBlock: new BlockNode(new List<StatementNode>(), 1, 5, "{}"),
                hasElse: false,
                elseBlock: null,
                line: 1, column: 1, text: "if (123) {}"
            );

            var rep = CheckNode(ifNode);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("'if' condition must be Boolean", rep.Errors[0].Message);
        }

        [Fact]
        public void Ternary_MismatchedArms_ReportsError()
        {
            var tern = new TernaryNode(
                condition: new LiteralNode(true, 1, 1, "true", SymbolType.Boolean),
                trueExpression: new LiteralNode(1L, 1, 7, "1", SymbolType.Integer),
                falseExpression: new LiteralNode("no", 1, 10, "\"no\"", SymbolType.Text),
                line: 1, column: 1, text: "true ? 1 : \"no\""
            );

            var rep = CheckNode(tern);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Ternary arms must have same type", rep.Errors[0].Message);
        }

        [Fact]
        public void DateLiteral_InvalidFormat_ReportsError()
        {
            var dateLit = new LiteralNode("2025-13-01", 1, 1, "\"2025-13-01\"", SymbolType.Date);
            var rep = CheckNode(dateLit);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Invalid date format: 2025-13-01. Expected ISO 8601 (yyyy-MM-dd)", rep.Errors[0].Message);
        }

        [Fact]
        public void MethodChain_ParseInvalidArg_ReportsError()
        {
            // Boolean.parse(123)
            var typeLit = new TypeLiteralNode(SymbolType.Boolean, 1, 1, "Boolean", SymbolType.Boolean);
            var argument = new LiteralNode(123L, 1, 15, "123", SymbolType.Integer);
            var chain = new MethodChainExpressionNode(
                left: typeLit,
                methodName: "parse",
                arguments: new List<ExpressionNode> { argument },
                next: null,
                line: 1, column: 1, text: "Boolean.parse(123)"
            );

            var rep = CheckNode(chain);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Cannot parse a Integer as Boolean", rep.Errors[0].Message);
        }

        [Fact]
        public void FunctionCall_WrongArgCount_ReportsError()
        {
            // function foo(a: Integer, b: Integer) { return 0; }
            var fn = new FunctionDefinitionNode(
                "foo", SymbolType.Integer,
                new FormalParameterListNode(new List<FormalParameterNode>
                {
                    new FormalParameterNode(SymbolType.Integer, "a", 1, 1, "Integer a"),
                    new FormalParameterNode(SymbolType.Integer, "b", 1, 1, "Integer b")
                }, 1, 1, "(Integer a, Integer b)"),
                new BlockNode(new List<StatementNode>
                {
                    new ReturnNode(new LiteralNode(0L, 1, 1, "0", SymbolType.Integer), 1, 1, "return 0;")
                }, 1, 1, "{}"),
                1, 1, "function foo(Integer a, Integer b) { return 0; }"
            );

            // call with 1 argument instead of 2
            var call = new FunctionCallNode(
                "foo",
                new List<ExpressionNode>
                {
                    new LiteralNode(1L, 2, 1, "1", SymbolType.Integer)
                },
                2, 1, "foo(1)"
            );
            call.Definition = fn;
            var stmt = new ExpressionStatementNode(call, 2, 1, "foo(1);");

            var rep = CheckProgram(new[] { fn }, new[] { stmt });
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("expects 2 args, but got 1 args", rep.Errors[0].Message);
        }

        [Fact]
        public void FunctionCall_ArgTypeMismatch_ReportsError()
        {
            // function bar(x: Text) { return ""; }
            var fn = new FunctionDefinitionNode(
                "bar", SymbolType.Text,
                new FormalParameterListNode(new List<FormalParameterNode>
                {
                    new FormalParameterNode(SymbolType.Text, "x", 1, 1, "Text x")
                }, 1, 1, "(Text x)"),
                new BlockNode(new List<StatementNode>
                {
                    new ReturnNode(new LiteralNode("", 1, 1, "\"\"", SymbolType.Text), 1, 1, "return \"\";")
                }, 1, 1, "{}"),
                1, 1, "function bar(Text x) { return \"\"; }"
            );

            // call bar(123)
            var call = new FunctionCallNode(
                "bar",
                new List<ExpressionNode>
                {
                    new LiteralNode(123L, 2, 1, "123", SymbolType.Integer)
                },
                2, 1, "bar(123)"
            );
            call.Definition = fn;
            var stmt = new ExpressionStatementNode(call, 2, 1, "bar(123);");

            var rep = CheckProgram(new[] { fn }, new[] { stmt });
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Argument 1 has type Integer, expected Text", rep.Errors[0].Message);
        }


        [Fact]
        public void ValidArithmetic_NoErrors()
        {
            var expr = new BinaryNode(
                new LiteralNode(2L, 1, 1, "2", SymbolType.Integer),
                new LiteralNode(3L, 1, 5, "3", SymbolType.Integer),
                OpType.Multiply,
                1, 1, "2 * 3"
            );
            var rep = CheckNode(expr);
            Assert.False(rep.HasErrors(ErrorType.TypeCheck));
        }

        [Fact]
        public void Equality_NonBasicType_ReportsError()
        {
            // simulate two schema identifiers both resolving to a SchemaDefinitionNode
            var schemaDef = new SchemaDefinitionNode(
                "S",
                new SchemaObjectNode(new Dictionary<string, SchemaField>(), 1, 1, "schema S {}"),
                1, 1, "schema S {}"
            );
            var leftId = new IdentifierNode("S", 1, 1, "S") { Definition = schemaDef };
            var rightId = new IdentifierNode("S", 1, 1, "S") { Definition = schemaDef };

            var eq = new BinaryNode(leftId, rightId, OpType.Equality, 1, 1, "S == S");
            var rep = CheckNode(eq);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Equality operations requires basic type operands", rep.Errors[0].Message);
        }


        [Fact]
        public void MethodChain_ParseOnNonType_ReportsError()
        {
            // ("123").parse()
            var chain = new MethodChainExpressionNode(
                left: new LiteralNode("123", 1, 1, "\"123\"", SymbolType.Text),
                methodName: "parse",
                arguments: new List<ExpressionNode>(),
                next: null,
                line: 1, column: 1, text: "\"123\".parse()"
            );

            var rep = CheckNode(chain);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("parse() must be invoked on a type literal", rep.Errors[0].Message);
        }


        [Fact]
        public void SchemaObject_UnknownFieldType_ReportsError()
        {
            var schemaObj = new SchemaObjectNode(
                fields: new Dictionary<string, SchemaField>
                {
                    ["f"] = new SchemaField("NonexistentType", "f")
                },
                line: 1, column: 1,
                text: "schema S { NonexistentType f; }"
            );
            var schema = new SchemaDefinitionNode(
                identifier: "S",
                objectNode: schemaObj,
                line: 1, column: 1,
                text: "schema S { NonexistentType f; }"
            );

            var rep = CheckNode(schema);

            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains(
                "Unknown type 'NonexistentType' in field 'f'",
                rep.Errors[0].Message
            );
        }


        [Fact]
        public void DatasetObject_MissingAdapter_ReportsError()
        {
            var props = new Dictionary<string, DatasetObjectNode.DatasetProperty>
            {
                ["source"] = new DatasetObjectNode.DatasetProperty(
                    "source",
                    new LiteralNode("file.csv", 1, 1, "\"file.csv\"", SymbolType.Text)
                ),
                ["schema"] = new DatasetObjectNode.DatasetProperty(
                    "schema",
                    new IdentifierNode("S", 1, 1, "S")
                )
            };
            var dsObj = new DatasetObjectNode(1, 1, "dataset D {}", props);
            var ds = new DatasetDefinitionNode("D", dsObj, 1, 1, "dataset D {}");

            var rep = CheckProgram(new[] { ds }, new StatementNode[0]);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Required property 'adapter' is missing", rep.Errors[0].Message);
        }


        [Fact]
        public void DatasetObject_UnsupportedAdapter_ReportsError()
        {
            var props = new Dictionary<string, DatasetObjectNode.DatasetProperty>
            {
                ["adapter"] = new DatasetObjectNode.DatasetProperty(
                    "adapter",
                    new LiteralNode("xml", 1, 1, "\"xml\"", SymbolType.Text)
                ),
                ["source"] = new DatasetObjectNode.DatasetProperty(
                    "source",
                    new LiteralNode("file.xml", 1, 1, "\"file.xml\"", SymbolType.Text)
                ),
                ["schema"] = new DatasetObjectNode.DatasetProperty(
                    "schema",
                    new IdentifierNode("S", 1, 1, "S")
                )
            };
            var dsObj = new DatasetObjectNode(1, 1, "dataset D {}", props);
            var ds = new DatasetDefinitionNode("D", dsObj, 1, 1, "dataset D {}");

            var rep = CheckProgram(new[] { ds }, new StatementNode[0]);
            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains("Unsupported adapter", rep.Errors[0].Message);
        }


        [Fact]
        public void MashdDefinition_LeftNotDataset_ReportsError()
        {
            var left = new LiteralNode("foo", 1, 1, "\"foo\"", SymbolType.Text);
            var right = new LiteralNode("bar", 1, 1, "\"bar\"", SymbolType.Text);

            var mashd = new MashdDefinitionNode(
                identifier: "id",
                left: left,
                right: right,
                line: 1, column: 1,
                text: "mashd id = \"foo\" mash \"bar\""
            );

            var rep = CheckNode(mashd);

            Assert.True(rep.HasErrors(ErrorType.TypeCheck));
            Assert.Contains(
                "Left side of mashd definition 'id' must be a Dataset, but got Text",
                rep.Errors[0].Message
            );
        }
    }
}