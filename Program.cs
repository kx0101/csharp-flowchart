using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using static System.Console;

var code = @"
public class Sample
{
    public static void Main(string[] args)
    {
        var y = 4 + 3;
        Test(y);
    }

    public void Test(int x)
    {
        if (x > 5)
        {
            Console.WriteLine(""Greater"");
        }
        else
        {
            Console.WriteLine(""Smaller"");
        }

        var message = ""vlakas"";
        Hix(message);
    }

    private void Hix(string message)
    {
        Console.WriteLine($""EIMAI O HIX, {message}"");
    }
}
";

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

string flowchart = "digraph Flowchart {\n";

int nodeId = 1;
int parentId = 0;
Dictionary<string, int> methodNodes = new();
Dictionary<string, string> variables = new();

foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    methodNodes[method.Identifier.Text] = nodeId;

    WriteLine(method.Identifier);
    if (nodeId == 1)
    {
        flowchart += $"    Node0 [shape=box, label=\"{method.Identifier.Text}()\"];\n";
    }

    foreach (var statement in method.Body.Statements)
    {
        flowchart += ProcessStatement(statement, ref nodeId, $"Node{parentId}", methodNodes, variables);
        parentId++;
    }
}

flowchart += "}\n";
File.WriteAllText("flowchart.dot", flowchart);
Process.Start("dot", "-Tpng flowchart.dot -o flowchart.png");

WriteLine("Flowchart generated: flowchart.dot & flowchart.png");

static string ProcessStatement(
    StatementSyntax statement,
    ref int nodeId,
    string parentNode,
    Dictionary<string, int> methodNodes,
    Dictionary<string, string> variables
)
{
    string output = "";
    int currentId = nodeId++;

    if (statement is LocalDeclarationStatementSyntax varDecl)
    {
        var variableName = varDecl.Declaration.Variables.First().Identifier.Text;
        var variableValue = varDecl.Declaration.Variables.First().Initializer?.Value.ToString() ?? "null";
        variables[variableName] = variableValue;

        if (varDecl.Declaration.Variables.First().Initializer?.Value is BinaryExpressionSyntax binaryExpr)
        {
            int evaluatedValue = EvaluateBinaryExpression(binaryExpr);
            variables[variableName] = evaluatedValue.ToString();

            output += $"    Node{currentId} [shape=ellipse, label=\"{variableName} = {varDecl.Declaration.Variables.First().Initializer.Value}\"];\n";
            output += $"    {parentNode} -> Node{currentId};\n";

            return output;
        }

        output += $"    Node{currentId} [shape=ellipse, label=\"{variableName} = {variableValue.Replace("\"", "\\\"")}\"];\n";
        output += $"    {parentNode} -> Node{currentId};\n";

        return output;
    }

    if (statement is ExpressionStatementSyntax exprStmt)
    {
        if (exprStmt.Expression is InvocationExpressionSyntax invocationExpr)
        {
            var invocationText = invocationExpr.ToString();
            foreach (var variable in variables)
            {
                if (invocationText.Contains($"{{{variable.Key}}}"))
                {
                    var rawValue = variable.Value.Trim('"');
                    invocationText = invocationText.Replace($"{{{variable.Key}}}", rawValue);
                }
                else
                {
                    invocationText = invocationText.Replace(variable.Key, variable.Value);
                }
            }

            output += $"    Node{currentId} [shape=box, label=\"{invocationText.Replace("\"", "\\\"")}\"];\n";
            output += $"    {parentNode} -> Node{currentId};\n";

            return output;
        }
    }

    if (statement is BlockSyntax blockStmt)
    {
        foreach (var stmt in blockStmt.Statements)
        {
            return ProcessStatement(stmt, ref nodeId, parentNode, methodNodes, variables);
        }
    }

    var statementText = statement.ToString();
    foreach (var variable in variables)
    {
        statementText = statementText.Replace(variable.Key, variable.Value);
    }

    output += $"    Node{currentId} [shape=box, label=\"{statementText.Replace("\"", "\\\"")}\"];\n";
    output += $"    {parentNode} -> Node{currentId};\n";

    return output;
}

static int EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpr)
{
    if (int.TryParse(binaryExpr.Left.ToString(), out int leftVal) &&
        int.TryParse(binaryExpr.Right.ToString(), out int rightVal))
    {
        return binaryExpr.OperatorToken.Text switch
        {
            "+" => leftVal + rightVal,
            "-" => leftVal - rightVal,
            "*" => leftVal * rightVal,
            "/" => rightVal != 0 ? leftVal / rightVal : 0,
            "%" => rightVal != 0 ? leftVal % rightVal : 0,
            _ => 0
        };
    }

    return 0;
}
