using Mashd.Backend.Adapters;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Backend.Interpretation;

public static class MashdHandler
{
    public static IValue CreateMashed(IAstVisitor<IValue> visitor, BinaryNode node)
    {
        var leftValue = node.Left.Accept(visitor);
        var rightValue = node.Right.Accept(visitor);

        if (leftValue is not DatasetValue leftDataset || rightValue is not DatasetValue rightDataset)
            throw new NotImplementedException(
                $"Combine operator not implemented for types {leftValue.GetType()} and {rightValue.GetType()}.");

        if (node.Left is not IdentifierNode nodeLeft || node.Right is not IdentifierNode nodeRight)
            throw new NotImplementedException(
                $"Combine operator not implemented for types {node.Left.GetType()} and {node.Right.GetType()}.");

        return new MashdValue(nodeLeft.Name, leftDataset, nodeRight.Name, rightDataset);
    }
}