using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpFlowchart.Builders;
using CSharpFlowchart.Parsers;

namespace CSharpFlowchart.CodeAnalyzer;

class CodeAnalyzer
{
    private string code = @"
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

    private FlowchartBuilder _flowchartBuilder;
    private Dictionary<string, int> _methodNodes = new();
    private Dictionary<string, string> _variables = new();
    private Dictionary<string, List<string>> _methodParameters = new();

    public void AnalyzeCode()
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        _flowchartBuilder = new FlowchartBuilder();

        AnalyzeMethods(root);

        _flowchartBuilder.GenerateOutput();
    }

    private void AnalyzeMethods(SyntaxNode root)
    {
        var nodeId = 1;
        var parentId = 0;
        var lastMethodNodeId = 0;

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            _methodNodes[method.Identifier.Text] = nodeId;

            var parameterNames = method.ParameterList.Parameters
                .Select(p => p.Identifier.Text)
                .ToList();
            _methodParameters[method.Identifier.Text] = parameterNames;

            if (nodeId == 1)
            {
                _flowchartBuilder.AddNode(0, $"{method.Identifier.Text}()", "box");
                lastMethodNodeId = 0;
            }

            parentId = lastMethodNodeId;

            for (int i = 0; i < method.Body.Statements.Count; i++)
            {
                var statement = method.Body.Statements[i];
                var statementProcessor = new Parser(_methodNodes, _variables);
                string result = statementProcessor.ProcessStatement(statement, ref nodeId, $"Node{parentId}");
                _flowchartBuilder.AppendRaw(result);

                parentId = nodeId - 1;
            }

            lastMethodNodeId = parentId;
        }
    }
}
