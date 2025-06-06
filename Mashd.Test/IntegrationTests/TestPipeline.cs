﻿using Mashd.Application;
using Mashd.Backend.Interpretation;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Test.IntegrationTests;

public static class TestPipeline
{
    /// <summary>
    /// Runs the full user pipeline (lex→parse→ast→resolve→typecheck) via MashdInterpreter,
    /// then evaluates with the Backend Interpreter to expose variable values.
    /// </summary>
    public static (Interpreter Interpreter, ProgramNode Ast) Run(string source)
    {
        // Use the application-facing pipeline for front-end phases
        var app = new MashdInterpreter(source)
            .Lex()
            .Parse()
            .BuildAst()
            .HandleImports()
            .Resolve()
            .TypeCheck();

        // Retrieve the ProgramNode AST that the application built
        var ast = app.Ast ?? throw new InvalidOperationException("AST not built");

        // Now run the backend evaluation on that AST
        var interpreter = new Interpreter();
        interpreter.Evaluate(ast);

        return (interpreter, ast);
    }
    
    /// <summary>
    /// Runs the *entire* pipeline (lex, parse, ast, imports, resolve, typecheck, interpret),
    /// letting either a FrontendException or a RuntimeException bubble out.
    /// </summary>
    public static void RunFull(string source)
    {
        var interp = new MashdInterpreter(source)
            .Lex()
            .Parse()
            .BuildAst()
            .HandleImports()
            .Resolve()
            .TypeCheck();

        interp.Interpret();  // may throw RuntimeException
    }

    /// <summary>Find the single VariableDeclarationNode with the given identifier.</summary>
    public static VariableDeclarationNode FindVar(ProgramNode ast, string name)
    {
        return ast.Statements
            .OfType<VariableDeclarationNode>()
            .Single(d => d.Identifier == name);
    }

    /// <summary>Get the raw Value for a named variable.</summary>
    public static IValue GetValue(Interpreter interpreter, ProgramNode ast, string name)
    {
        var decl = FindVar(ast, name);
        return interpreter.Values[decl];
    }

    /// <summary>Get the integer (long) content of a variable.</summary>
    public static long GetInteger(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is IntegerValue iv) return iv.Raw;
        throw new Exception($"Variable '{name}' is not an IntegerValue");
    }

    /// <summary>Get the decimal (double) content of a variable.</summary>
    public static double GetDecimal(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is DecimalValue dv) return dv.Raw;
        if (v is IntegerValue iv) return iv.Raw;
        throw new Exception($"Variable '{name}' is not a DecimalValue or IntegerValue");
    }

    /// <summary>Get the string content of a variable.</summary>
    public static string GetText(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is TextValue sv) return sv.Raw;
        throw new Exception($"Variable '{name}' is not a StringValue");
    }

    /// <summary>Get the boolean content of a variable.</summary>
    public static bool GetBoolean(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is BooleanValue bv) return bv.Raw;
        throw new Exception($"Variable '{name}' is not a BooleanValue");
    }
    /// <summary>Get the schema Value for a named variable.</summary>
    public static SchemaValue GetSchema(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is ObjectValue ov) return BuildSchemaFromObject(ov);
        throw new Exception($"Variable '{name}' is not a SchemaValue");
    }
    
    private static SchemaValue BuildSchemaFromObject(ObjectValue value)
    {
        var fields = new Dictionary<string, SchemaFieldValue>();

        foreach (var (identifier, fieldValue) in value.Raw)
        {
            if (fieldValue is not ObjectValue fieldObjectValue) continue;

            fields[identifier] = BuildSchemaFieldValueFromObject(fieldObjectValue);
        }

        return new SchemaValue(fields);
    }

    private static SchemaFieldValue BuildSchemaFieldValueFromObject(ObjectValue value)
    {
        var name = value.Raw.GetValueOrDefault("name");
        var type = value.Raw.GetValueOrDefault("type");

        if (name is not TextValue textValue || type is not TypeValue typeValue)
            throw new ArgumentException("Invalid object value for schema field.");
        
        return new SchemaFieldValue(typeValue.Raw, textValue.Raw);
    }

    /// <summary>Get the dataset Value for a named variable.</summary>
    public static DatasetValue GetDataset(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is DatasetValue dv) return dv;
        throw new Exception($"Variable '{name}' is not a DatasetValue");
    }

    /// <summary>Get the mashd Value for a named variable.</summary>
    public static MashdValue GetMashd(Interpreter interpreter, ProgramNode ast, string name)
    {
        var v = GetValue(interpreter, ast, name);
        if (v is MashdValue mv) return mv;
        throw new Exception($"Variable '{name}' is not a MashdValue");
    }

}