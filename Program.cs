using CSharpFlowchart.CodeAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        string filePath;

        if (args.Length > 0)
        {
            filePath = args[0];
        }
        else
        {
            Console.Write("Enter path to C# file to analyze: ");
            filePath = Console.ReadLine();
        }

        try
        {
            var codeAnalyzer = new CodeAnalyzer(filePath);
            codeAnalyzer.AnalyzeCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
