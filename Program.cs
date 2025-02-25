using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using static System.Console;

var code = @"
public class Sample
{
    public static void Main(string[] args)
    {
        var x = 4 + 3;
        Test(x);
    }

    public void Test(int x)
    {
        var z = 1;

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
int lastMethodNodeId = 0;
Dictionary<string, int> methodNodes = new();
Dictionary<string, string> variables = new();
Dictionary<string, List<string>> methodParameters = new();

foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    methodNodes[method.Identifier.Text] = nodeId;
    var parameterNames = method.ParameterList.Parameters
        .Select(p => p.Identifier.Text)
        .ToList();
    methodParameters[method.Identifier.Text] = parameterNames;

    if (nodeId == 1)
    {
        flowchart += $"    Node0 [shape=box, label=\"{method.Identifier.Text}()\"];\n";
        lastMethodNodeId = 0;
    }

    parentId = lastMethodNodeId;

    for (int i = 0; i < method.Body.Statements.Count; i++)
    {
        var statement = method.Body.Statements[i];
        var result = ProcessStatement(statement, ref nodeId, $"Node{parentId}", methodNodes, variables);
        flowchart += result;

        parentId = nodeId - 1;
    }

    lastMethodNodeId = parentId;
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
        }

        output += $"    Node{currentId} [shape=ellipse, label=\"{variableName} = {variableValue.Replace("\"", "\\\"")}\"];\n";
        output += $"    {parentNode} -> Node{currentId};\n";

        return output;
    }

    if (statement is ExpressionStatementSyntax exprStmt &&
        exprStmt.Expression is InvocationExpressionSyntax invocationExpr)
    {
        var invocationText = invocationExpr.ToString();

        foreach (var variable in variables)
        {
            foreach (var arg in invocationExpr.ArgumentList.Arguments)
            {
                if (arg.Expression is IdentifierNameSyntax argName &&
                    argName.Identifier.Text == variable.Key)
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
            }
        }

        foreach (var variable in variables)
        {
            if (invocationText.Contains($"{{{variable.Key}}}"))
            {
                var rawValue = variable.Value.Trim('"');
                invocationText = invocationText.Replace($"{{{variable.Key}}}", rawValue);
            }
        }

        output += $"    Node{currentId} [shape=box, label=\"{invocationText.Replace("\"", "\\\"")}\"];\n";
        output += $"    {parentNode} -> Node{currentId};\n";

        return output;
    }

    if (statement is IfStatementSyntax ifStmt)
    {
        var conditionText = ifStmt.Condition.ToString();
        output += $"    {parentNode} -> Node{currentId} [label=\"{conditionText}\"];\n";

        foreach (var variable in variables)
        {
            conditionText = conditionText.Replace(variable.Key, variable.Value);
        }

        output += $"    Node{currentId} [shape=diamond, label=\"{conditionText}\"];\n";

        int ifNodeId = currentId;
        int lastNodeId = ifNodeId;

        if (EvaluateCondition(conditionText))
        {
            if (ifStmt.Statement is BlockSyntax block)
            {
                foreach (var blockStatement in block.Statements)
                {
                    output += ProcessStatement(blockStatement, ref nodeId, $"Node{lastNodeId}", methodNodes, variables);
                    lastNodeId = nodeId - 1;
                }
            }
            else
            {
                output += ProcessStatement(ifStmt.Statement, ref nodeId, $"Node{ifNodeId}", methodNodes, variables);
                lastNodeId = nodeId - 1;
            }
        }
        else if (ifStmt?.Else != null)
        {
            if (ifStmt.Else.Statement is BlockSyntax block)
            {
                foreach (var blockStatement in block.Statements)
                {
                    output += ProcessStatement(blockStatement, ref nodeId, $"Node{lastNodeId}", methodNodes, variables);
                    lastNodeId = nodeId - 1;
                }
            }
            else
            {
                output += ProcessStatement(ifStmt.Else.Statement, ref nodeId, $"Node{ifNodeId}", methodNodes, variables);
                lastNodeId = nodeId - 1;
            }
        }

        nodeId = lastNodeId + 1;
        return output;
    }

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

static bool EvaluateCondition(string condition)
{
    try
    {
        var dataTable = new System.Data.DataTable();
        return Convert.ToBoolean(dataTable.Compute(condition, ""));
    }
    catch
    {
        WriteLine("Condition evaluation failed.");
        return false;
    }
}
