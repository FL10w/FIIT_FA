using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Core;

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
        if (child != null)
        {
            Splay(child);
        }
        else if (parent != null)
        {
            Splay(parent);
        }
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }
        
        BstNode<TKey, TValue>? lastVisited = FindLastVisited(key);
        if (lastVisited != null)
        {
            Splay(lastVisited);
        }
        
        value = default!;
        return false;
    }

    public override bool ContainsKey(TKey key)  
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        
        if (node != null)
        {
            Splay(node);
            return true;
        }
        
        return false;
    }

    public override bool Remove(TKey key)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node == null)
        {
            return false;
        }

        Splay(node);

        BstNode<TKey, TValue>? leftSubtree = Root?.Left;
        BstNode<TKey, TValue>? rightSubtree = Root?.Right;

        if (leftSubtree == null)
        {
            Root = rightSubtree;
            if (Root != null)
                Root.Parent = null;
        }
        else if (rightSubtree == null)
        {
            Root = leftSubtree;
            if (Root != null)
                Root.Parent = null;
        }
        else
        {
            BstNode<TKey, TValue>? maxLeft = FindMax(leftSubtree);
            Splay(maxLeft!);
            Root!.Right = rightSubtree;
            if (rightSubtree != null)
                rightSubtree.Parent = Root;
        }

        Count--;
        return true;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            BstNode<TKey, TValue> parent = node.Parent;
            BstNode<TKey, TValue>? grandParent = parent.Parent;

            if (grandParent == null)
            {
                if (node.IsLeftChild)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else if (node.IsLeftChild && parent.IsLeftChild)
            {
                RotateRight(grandParent);
                RotateRight(parent);
            }
            else if (node.IsRightChild && parent.IsRightChild)
            {
                RotateLeft(grandParent);
                RotateLeft(parent);
            }
            else if (node.IsLeftChild && parent.IsRightChild)
            {
                RotateRight(parent);
                RotateLeft(grandParent);
            }
            else
            {
                RotateLeft(parent);
                RotateRight(grandParent);
            }
        }

        Root = node;
    }

    private BstNode<TKey, TValue>? FindLastVisited(TKey key)
    {
        BstNode<TKey, TValue>? current = Root;
        BstNode<TKey, TValue>? lastVisited = null;

        while (current != null)
        {
            lastVisited = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
                return current;
            current = cmp < 0 ? current.Left : current.Right;
        }

        return lastVisited;
    }

    private BstNode<TKey, TValue>? FindMax(BstNode<TKey, TValue> node)
    {
        while (node?.Right != null)
        {
            node = node.Right;
        }
        return node;
    }
}