using System.Diagnostics;
using System.Text;

namespace CSharpFlowchart.Builders;

class FlowchartBuilder
{
    private StringBuilder _flowchart = new StringBuilder("digraph Flowchart {\n");
    private string _outputPath;

    public FlowchartBuilder(string outputPath)
    {
        _outputPath = outputPath;
    }

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
        var dotFilePath = $"{_outputPath}_flowchart.dot";
        var pngFilePath = $"{_outputPath}_flowchart.png";

        File.WriteAllText(dotFilePath, flowchartContent);

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = $"-Tpng \"{dotFilePath}\" -o \"{pngFilePath}\"",
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    Console.WriteLine($"Warning: Graphviz 'dot' command failed: {error}");
                    Console.WriteLine("Flowchart DOT file was generated but PNG conversion failed.");
                    Console.WriteLine("Make sure Graphviz is installed and 'dot' is in your PATH.");
                }
                else
                {
                    Console.WriteLine($"Flowchart generated successfully:");
                    Console.WriteLine($"- DOT file: {dotFilePath}");
                    Console.WriteLine($"- PNG file: {pngFilePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Couldn't run Graphviz 'dot' command: {ex.Message}");
            Console.WriteLine($"Flowchart DOT file was generated at: {dotFilePath}");
            Console.WriteLine("Make sure Graphviz is installed to generate the PNG visualization.");
        }
    }
}
