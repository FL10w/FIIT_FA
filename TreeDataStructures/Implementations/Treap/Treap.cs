﻿using System.Diagnostics.CodeAnalysis;
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
        if (root == null) return (null, null);
        if (root.Key.CompareTo(key) <= 0)
        {
            var (l, r) = Split(root.Right, key);
            root.Right = l;
            if (l != null) l.Parent = root;
            return (root, r);
        }
        else
        {
            var(l, r) = Split(root.Left, key);
            root.Left = r;
            if (r != null) r.Parent = root;
            return(l, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) left.Right.Parent = left;
            return left;
        }

        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) right.Left.Parent = right;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var ex = FindNode(key);
        if (ex != null)
        {
            ex.Value = value;
            return;
        }
        var (left, right) = Split(Root, key);
        var newNode = CreateNode(key, value);
        Root = Merge(Merge(left, newNode), right);
        if (Root != null) Root.Parent = null;
        Count++;
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null) return false;
        var merged = Merge(node.Left, node.Right);
        if (node.Parent == null) Root = merged;
        else if (node.Parent.Left == node) node.Parent.Left = merged;
        else node.Parent.Right = merged;
        if (merged != null) merged.Parent = node.Parent;
        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value)
        {
            Priority = Random.Shared.Next()
        };
    }

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) {}
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) {}
    
}