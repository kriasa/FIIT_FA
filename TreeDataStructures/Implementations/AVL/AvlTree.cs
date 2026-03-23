using TreeDataStructures.Core;
using TreeDataStructures.Implementations.Treap;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        RebalancePath(newNode.Parent);
    }
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey,TValue>? child)
    {
        RebalancePath(parent);
    }

    private void RebalancePath(AvlNode<TKey,TValue>? node)
    {
        while (node is not null)
        {
            UpdateHeight(node);
            if (IsUnbalance(node)){
                Balance(node);
            }
            node = node.Parent;
        }
    }
    private int GetHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;
    private int GetBalance(AvlNode<TKey, TValue> node) => GetHeight(node.Right) - GetHeight(node.Left);
    private void UpdateHeight(AvlNode<TKey,TValue> node) => node.Height = 1 + Math.Max(GetHeight(node.Left),GetHeight(node.Right));
    private bool IsUnbalance(AvlNode<TKey,TValue> node) => Math.Abs(GetBalance(node))>1;
    private void Balance(AvlNode<TKey, TValue> node)
    {
        int balanceFactor = GetBalance(node);

        switch (balanceFactor)
        {
            case > 1:
            
                var r = node.Right!;
                if (GetBalance(r) < 0)
                {
                    var rl = r.Left;
                    RotateBigLeft(node);

                    UpdateHeight(node);
                    UpdateHeight(r);
                    if (rl != null) UpdateHeight(rl);
                }
                else
                {
                    RotateLeft(node);
                    UpdateHeight(node);
                    UpdateHeight(r);
                }
                break;

            case < -1:

                var l = node.Left!;
                if (GetBalance(l) > 0)
                {
                    var lr = l.Right;
                    RotateBigRight(node);

                    UpdateHeight(node);
                    UpdateHeight(l);
                    if (lr != null) UpdateHeight(lr);
                }
                else
                {
                    RotateRight(node);
                    UpdateHeight(node);
                    UpdateHeight(l);
                }
                break;
            default:
                break;
        }
    }
}