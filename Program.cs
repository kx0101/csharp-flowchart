using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using static System.Console;

var code = @"
public class Sample
{
    public static void Main(string[] args)
    {
        Test(5);
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

        Hix();
    }

    private void Hix()
    {
        Console.WriteLine(""EIMAI O HIX"");
    }
}
";

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

string flowchart = "digraph Flowchart {\n";

int nodeId = 0;
Dictionary<string, int> methodNodes = new();

foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    methodNodes[method.Identifier.Text] = nodeId;

    flowchart += $"    Node{nodeId} [shape=box, label=\"{method.Identifier.Text}()\"];\n";

    foreach (var statement in method.Body.Statements)
    {
        flowchart += ProcessStatement(statement, ref nodeId, $"Node{nodeId}", methodNodes);
    }
}

flowchart += "}\n";

File.WriteAllText("flowchart.dot", flowchart);

Process.Start("dot", "-Tpng flowchart.dot -o flowchart.png");

Console.WriteLine("Flowchart generated: flowchart.dot & flowchart.png");

static string ProcessStatement(
    StatementSyntax statement,
    ref int nodeId,
    string parentNode,
    Dictionary<string, int> methodNodes
)
{
    string output = "";

    if (statement is IfStatementSyntax ifStmt)
    {
        int currentId = nodeId++;
        output += $"    Node{currentId} [shape=diamond, label=\"{ifStmt.Condition.ToString().Replace("\"", "\\\"")}\"];\n";
        output += $"    {parentNode} -> Node{currentId};\n";

        int thenId = nodeId++;
        output += $"    Node{currentId} -> Node{thenId} [label=\"Yes\"];\n";

        if (ifStmt.Statement is BlockSyntax ifBlock)
        {
            foreach (var stmt in ifBlock.Statements)
            {
                output += ProcessStatement(stmt, ref nodeId, $"Node{thenId}", methodNodes);
            }
        }
        else
        {
            output += ProcessStatement(ifStmt.Statement, ref nodeId, $"Node{thenId}", methodNodes);
        }

        if (ifStmt.Else != null)
        {
            int elseId = nodeId++;
            output += $"    Node{currentId} -> Node{elseId} [label=\"No\"];\n";

            if (ifStmt.Else.Statement is BlockSyntax elseBlock)
            {
                foreach (var stmt in elseBlock.Statements)
                {
                    output += ProcessStatement(stmt, ref nodeId, $"Node{elseId}", methodNodes);
                }
            }
            else
            {
                output += ProcessStatement(ifStmt.Else.Statement, ref nodeId, $"Node{elseId}", methodNodes);
            }
        }
    }

    if (statement is BlockSyntax blockStmt)
    {
        foreach (var stmt in blockStmt.Statements)
        {
            output += ProcessStatement(stmt, ref nodeId, parentNode, methodNodes);
        }
    }

    if (statement is ExpressionStatementSyntax exprStmt2)
    {
        int currentId = nodeId;
        WriteLine(exprStmt2);
        output += $"    Node{currentId} [shape=box, label=\"{exprStmt2.ToString().Replace("\"", "\\\"")}\"];\n";
        output += $"    {parentNode} -> Node{currentId};\n";

        nodeId++;
    }

    return output;
}
