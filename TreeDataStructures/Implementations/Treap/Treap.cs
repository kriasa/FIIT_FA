using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root is null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);
        root.Parent = null;

        if(cmp <= 0)
        {
            var (left, right) = Split(root.Right, key);
            root.Right = left;
            left?.Parent = root;
            return (root, right);
        }
        else
        {
            var (left, right) = Split(root.Left, key);
            root.Left = right;
            right?.Parent = root;
            return (left, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left is null) return right;
        if (right is null) return left;

        if (left.Priority <= right.Priority)
        {
            right.Left = Merge(left,right.Left);
            right.Left?.Parent = right;
            right.Parent = null;
            return right;
        }
        else
        {
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
            left.Parent = null;
            return left;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var node = FindNode(key);
        if (node is not null)
        {
            node.Value = value;
            return;
        }

        var newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);

        Root = Merge(Merge(left,newNode),right);
        Root?.Parent = null;

        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node is null) return false;

        var left = node.Left;
        var right = node.Right;

        var merge = Merge(left, right);
        Transplant(node,merge);

        Count--;
        OnNodeRemoved(null,Root);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey,TValue>(key,value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
    
}