using System.Collections.Generic;
using Esprima;
using Esprima.Ast;
using Esprima.Utils;

namespace JintDebuggerExample;

/// <summary>
/// Collects valid breakpoint locations. Eventually, this should probably be part of Jint itself, since it's really
/// Jint that makes the decision about what locations are valid.
/// </summary>
public class BreakPointCollector : AstVisitor
{
    private readonly List<Position> positions = new();

    public List<Position> Positions => positions;

    public BreakPointCollector()
    {
    }

    public override void Visit(Node node)
    {
        if (node is Statement && node is not BlockStatement)
        {
            positions.Add(node.Location.Start);
        }
        base.Visit(node);
    }

    protected override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
    {
        base.VisitDoWhileStatement(doWhileStatement);

        positions.Add(doWhileStatement.Test.Location.Start);
    }

    protected override void VisitForInStatement(ForInStatement forInStatement)
    {
        base.VisitForInStatement(forInStatement);

        positions.Add(forInStatement.Left.Location.Start);
    }

    protected override void VisitForOfStatement(ForOfStatement forOfStatement)
    {
        base.VisitForOfStatement(forOfStatement);

        positions.Add(forOfStatement.Left.Location.Start);
    }

    protected override void VisitForStatement(ForStatement forStatement)
    {
        base.VisitForStatement(forStatement);

        if (forStatement.Test != null)
        {
            positions.Add(forStatement.Test.Location.Start);
        }
        if (forStatement.Update != null)
        {
            positions.Add(forStatement.Update.Location.Start);
        }
    }

    protected override void VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
    {
        base.VisitArrowFunctionExpression(arrowFunctionExpression);

        positions.Add(arrowFunctionExpression.Body.Location.End);
    }

    protected override void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        base.VisitFunctionDeclaration(functionDeclaration);

        positions.Add(functionDeclaration.Body.Location.End);
    }

    protected override void VisitFunctionExpression(IFunction function)
    {
        base.VisitFunctionExpression(function);

        positions.Add(function.Body.Location.End);
    }
}
