using System.ComponentModel;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new RbNode<TKey, TValue>(key, value);
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (newNode.Parent is null)
        {
            newNode.Color = RbColor.Black;
            return;

        }else if (GetColor(newNode.Parent) == RbColor.Black)
        {
            return;
        }
        a
        FixInsert(newNode);
        Root!.Color = RbColor.Black;
    }
    private RbColor GetColor(RbNode<TKey,TValue>? node) => node?.Color ?? RbColor.Black;
    private void FixInsert(RbNode<TKey,TValue> node)
    {
        var parent = node.Parent;

        if (parent is null || GetColor(parent) == RbColor.Black) return;

        var grparent = parent.Parent;

        var uncle = (parent.IsLeftChild) ? grparent!.Right : grparent!.Left;

        if (GetColor(uncle) == RbColor.Red)
        {
            parent.Color = RbColor.Black;
            uncle!.Color = RbColor.Black;
            if (grparent.Parent is not null){
                grparent.Color = RbColor.Red;
                FixInsert(grparent);
            }
        }
        else if (GetColor(uncle) == RbColor.Black)
        {
            if (parent.IsLeftChild)
            {
                if (node.IsLeftChild)
                {
                    RotateRight(grparent);
                    parent.Color = RbColor.Black;
                }
                else
                {
                    RotateBigRight(grparent);
                    node.Color = RbColor.Black;
                }
            }
            else if (parent.IsRightChild)
            {
                if (node.IsRightChild)
                {
                    RotateLeft(grparent);
                    parent.Color = RbColor.Black;
                }
                else
                {
                    RotateBigLeft(grparent);
                    node.Color = RbColor.Black;
                }
            }
            grparent.Color = RbColor.Red;
        }
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (parent is null)
        {
            if (child is not null && child == Root) Root!.Color = RbColor.Black;
            return;
        }

        if (GetColor(child) == RbColor.Red)
        {
            child!.Color = RbColor.Black;
            return;
        }
        FixRemoved(parent,child);
    }
    private void FixRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? node)
    {
        if (parent is null)
        {
            node?.Color = RbColor.Black;
            return;
        }

        if (node == parent.Left)
        {
            var brother = parent.Right;

            if (GetColor(brother) == RbColor.Red)
            {
                RotateLeft(parent);
                parent.Color = RbColor.Red;
                brother!.Color = RbColor.Black;
                brother = parent.Right;
            }

            var nephew = brother?.Left;
            var fnephew = brother?.Right;

            if (GetColor(nephew) == RbColor.Black && GetColor(fnephew) == RbColor.Black)
            {
                brother?.Color = RbColor.Red;
                if (GetColor(parent) == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                }
                else
                {
                    FixRemoved(parent.Parent, parent);
                }
            }
            else if (GetColor(nephew) == RbColor.Red)
            {
                RotateBigLeft(parent);
                nephew?.Color = parent.Color;
                parent.Color = RbColor.Black;
            }
            else if (GetColor(fnephew) == RbColor.Red)
            {
                RotateLeft(parent);
                brother?.Color = parent.Color;
                fnephew?.Color = RbColor.Black;
                parent.Color = RbColor.Black;
            }
        }
        else
        {
            var brother = parent.Left;

            if (GetColor(brother) == RbColor.Red)
            {
                RotateRight(parent);
                parent.Color = RbColor.Red;
                brother!.Color = RbColor.Black;
                brother = parent.Left;
            }

            var nephew = brother?.Right;
            var fnephew = brother?.Left;

            if (GetColor(nephew) == RbColor.Black && GetColor(fnephew) == RbColor.Black)
            {
                brother?.Color = RbColor.Red;
                if (GetColor(parent) == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                }
                else
                {
                    FixRemoved(parent.Parent, parent);
                }
            }
            else if (GetColor(nephew) == RbColor.Red)
            {
                RotateBigRight(parent);
                nephew?.Color = parent.Color;
                parent.Color = RbColor.Black;
            }
            else if (GetColor(fnephew) == RbColor.Red)
            {
                RotateRight(parent);
                brother?.Color = parent.Color;
                fnephew?.Color = RbColor.Black;
                parent.Color = RbColor.Black;
            }
        }
    }
}