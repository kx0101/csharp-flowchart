using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpFlowchart.ExpressionEvaluator;

static class ExpressionExtensions
{
    public static int EvaluateBinaryExpression(BinaryExpressionSyntax binaryExpr)
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

    public static bool EvaluateCondition(string condition)
    {
        try
        {
            var dataTable = new System.Data.DataTable();
            return Convert.ToBoolean(dataTable.Compute(condition, ""));
        }
        catch
        {
            Console.WriteLine("Condition evaluation failed.");
            return false;
        }
    }
}

