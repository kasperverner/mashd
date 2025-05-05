namespace TestProject1;

public static class TestSnippets
{
    public static string Get(string key)
    {
        return key switch
        {
            "FunctionDeclaration" => FunctionDeclaration,
            "IfStatement" => IfStatement,
            "VariableDeclaration" => VariableDeclaration,
            "Assignment" => Assignment,
            "ReturnStatement" => ReturnStatement,
            _ => throw new ArgumentException($"No snippet found for key: {key}")
        };
    }
    
    public const string FunctionDeclaration = "Boolean isEven(int x) { return x % 2 == 0; }";
    public const string IfStatement = "if (true) {return 1;}";
    public const string VariableDeclaration = "Integer x = 5;";
    public const string Assignment = "x = 10;";
    public const string ReturnStatement = "return 7;";
    
}