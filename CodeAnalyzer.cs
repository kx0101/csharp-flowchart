using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpFlowchart.Builders;
using CSharpFlowchart.Parsers;

namespace CSharpFlowchart.CodeAnalyzer;

class CodeAnalyzer
{
    private readonly string _code;
    private FlowchartBuilder _flowchartBuilder;
    private Dictionary<string, int> _methodNodes = new();
    private Dictionary<string, string> _variables = new();
    private Dictionary<string, List<string>> _methodParameters = new();
    private string _outputPath;

    public CodeAnalyzer(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' does not exist.");
        }

        _code = File.ReadAllText(filePath);

        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        _outputPath = Path.Combine(directory, fileName);
    }

    public void AnalyzeCode()
    {
        Console.WriteLine("Parsing code...");
        var tree = CSharpSyntaxTree.ParseText(_code);
        var root = tree.GetRoot();

        _flowchartBuilder = new FlowchartBuilder(_outputPath);

        Console.WriteLine("Analyzing methods...");
        AnalyzeMethods(root);

        Console.WriteLine("Generating flowchart...");
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

                var result = statementProcessor.ProcessStatement(statement, ref nodeId, $"Node{parentId}");
                _flowchartBuilder.AppendRaw(result);

                parentId = nodeId - 1;
            }

            lastMethodNodeId = parentId;
        }
    }
}
