using System.Diagnostics;
using System.Text;

namespace CSharpFlowchart.Builders;

class FlowchartBuilder
{
    private StringBuilder _flowchart = new StringBuilder("digraph Flowchart {\n");

    public void AddNode(int id, string label, string shape)
    {
        _flowchart.AppendLine($"    Node{id} [shape={shape}, label=\"{label}\"];");
    }

    public void AddEdge(string fromNode, string toNode, string label = "")
    {
        if (string.IsNullOrEmpty(label))
        {
            _flowchart.AppendLine($"    {fromNode} -> {toNode};");
        }
        else
        {
            _flowchart.AppendLine($"    {fromNode} -> {toNode} [label=\"{label}\"];");
        }
    }

    public void AppendRaw(string content)
    {
        _flowchart.Append(content);
    }

    public void GenerateOutput()
    {
        _flowchart.AppendLine("}");

        var flowchartContent = _flowchart.ToString();
        File.WriteAllText("flowchart.dot", flowchartContent);

        Process.Start("dot", "-Tpng flowchart.dot -o flowchart.png");
        Console.WriteLine("Flowchart generated: flowchart.dot & flowchart.png");
    }
}
