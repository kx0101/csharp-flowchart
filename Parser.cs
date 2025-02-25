using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpFlowchart.ExpressionEvaluator;

namespace CSharpFlowchart.Parsers;

class Parser
{
    private Dictionary<string, int> _methodNodes;
    private Dictionary<string, string> _variables;

    public Parser(Dictionary<string, int> methodNodes, Dictionary<string, string> variables)
    {
        _methodNodes = methodNodes;
        _variables = variables;
    }

    public string ProcessStatement(
        StatementSyntax statement,
        ref int nodeId,
        string parentNode
    )
    {
        var output = "";
        var currentId = nodeId++;

        if (statement is LocalDeclarationStatementSyntax varDecl)
        {
            output += ProcessVariableDeclaration(varDecl, currentId, parentNode);
        }
        else if (statement is ExpressionStatementSyntax exprStmt &&
                exprStmt.Expression is InvocationExpressionSyntax invocationExpr)
        {
            output += ProcessInvocation(invocationExpr, currentId, parentNode);
        }
        else if (statement is IfStatementSyntax ifStmt)
        {
            output += ProcessIfStatement(ifStmt, ref nodeId, currentId, parentNode);
        }

        return output;
    }

    private string ProcessVariableDeclaration(
        LocalDeclarationStatementSyntax varDecl,
        int currentId,
        string parentNode)
    {
        var variableName = varDecl.Declaration.Variables.First().Identifier.Text;
        var variableValue = varDecl.Declaration.Variables.First().Initializer?.Value.ToString() ?? "null";
        _variables[variableName] = variableValue;

        if (varDecl.Declaration.Variables.First().Initializer?.Value is BinaryExpressionSyntax binaryExpr)
        {
            int evaluatedValue = ExpressionExtensions.EvaluateBinaryExpression(binaryExpr);
            _variables[variableName] = evaluatedValue.ToString();
        }

        return $"    Node{currentId} [shape=ellipse, label=\"{variableName} = {variableValue.Replace("\"", "\\\"")}\"];\n" +
               $"    {parentNode} -> Node{currentId};\n";
    }

    private string ProcessInvocation(
        InvocationExpressionSyntax invocationExpr,
        int currentId,
        string parentNode)
    {
        var invocationText = invocationExpr.ToString();

        foreach (var variable in _variables)
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

        foreach (var variable in _variables)
        {
            if (invocationText.Contains($"{{{variable.Key}}}"))
            {
                var rawValue = variable.Value.Trim('"');
                invocationText = invocationText.Replace($"{{{variable.Key}}}", rawValue);
            }
        }

        return $"    Node{currentId} [shape=box, label=\"{invocationText.Replace("\"", "\\\"")}\"];\n" +
               $"    {parentNode} -> Node{currentId};\n";
    }

    private string ProcessIfStatement(
        IfStatementSyntax ifStmt,
        ref int nodeId,
        int currentId,
        string parentNode)
    {
        var conditionText = ifStmt.Condition.ToString();
        var output = $"    {parentNode} -> Node{currentId} [label=\"{conditionText}\"];\n" +
                      $"    Node{currentId} [shape=diamond, label=\"{conditionText}\"];\n";

        foreach (var variable in _variables)
        {
            conditionText = conditionText.Replace(variable.Key, variable.Value);
        }

        var ifNodeId = currentId;
        var lastNodeId = ifNodeId;

        if (ExpressionExtensions.EvaluateCondition(conditionText))
        {
            if (ifStmt.Statement is BlockSyntax block)
            {
                foreach (var blockStatement in block.Statements)
                {
                    output += ProcessStatement(blockStatement, ref nodeId, $"Node{lastNodeId}");
                    lastNodeId = nodeId - 1;
                }
            }
            else
            {
                output += ProcessStatement(ifStmt.Statement, ref nodeId, $"Node{ifNodeId}");
                lastNodeId = nodeId - 1;
            }
        }
        else if (ifStmt?.Else != null)
        {
            if (ifStmt.Else.Statement is BlockSyntax block)
            {
                foreach (var blockStatement in block.Statements)
                {
                    output += ProcessStatement(blockStatement, ref nodeId, $"Node{lastNodeId}");
                    lastNodeId = nodeId - 1;
                }
            }
            else
            {
                output += ProcessStatement(ifStmt.Else.Statement, ref nodeId, $"Node{ifNodeId}");
                lastNodeId = nodeId - 1;
            }
        }

        nodeId = lastNodeId + 1;
        return output;
    }
}
