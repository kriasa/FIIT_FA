using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent is not null) Splay(parent);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }
    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        return false;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent is not null)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;

            if (grandparent is null)
            {
                if (node.IsLeftChild)
                {
                    RotateRight(parent);
                }
                else
                {
                    RotateLeft(parent);
                }
            }
            else
            {
                if (node.IsLeftChild && parent.IsLeftChild)
                {
                    RotateDoubleRight(grandparent);
                }
                else if (node.IsRightChild && parent.IsRightChild)
                {
                    RotateDoubleLeft(grandparent);
                }
                else if (node.IsLeftChild && parent.IsRightChild)
                {
                    RotateBigLeft(grandparent);
                }
                else
                {
                    RotateBigRight(grandparent);
                }
            }
        }
        Root = node;
    }

}
