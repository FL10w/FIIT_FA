using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    => new (key, value);
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (Root == newNode)
        {
            newNode.Color = RbColor.Black;
            return;
        }
        if (newNode.Parent.Color == RbColor.Black) return;

        RbNode<TKey, TValue> current = newNode;
        while (current != Root && current.Parent.Color == RbColor.Red) 
        {
            RbNode<TKey, TValue> parent = current.Parent;
            RbNode<TKey, TValue> grandparent = parent.Parent;
            RbNode<TKey, TValue>? uncle;
            if (parent == grandparent.Left)
            {
                uncle = grandparent.Right;
                if (uncle != null && uncle.Color == RbColor.Red) 
                {
                    uncle.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    parent.Color = RbColor.Black;
                    current = grandparent;
                }

                else 
                {
                    if (current == parent.Left)
                    {
                        RotateRight(grandparent);
                        parent.Color = RbColor.Black;
                        grandparent.Color = RbColor.Red;
                        break;
                    }
                    else if (current == parent.Right)
                    {
                        RotateLeft(parent);
                        RotateRight(grandparent);
                        current.Color = RbColor.Black;
                        grandparent.Color = RbColor.Red;
                        break;
                    }

                }
            }
            else if (parent == grandparent.Right)
            {
                uncle = grandparent.Left;
                if (uncle != null && uncle.Color == RbColor.Red) 
                {
                    uncle.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    parent.Color = RbColor.Black;
                    current = grandparent;
                }
                else 
                {
                    if (current == parent.Right)
                    {
                        RotateLeft(grandparent);
                        parent.Color = RbColor.Black;
                        grandparent.Color = RbColor.Red;
                        break;
                    }
                    else if (current == parent.Left)
                    {
                        RotateRight(parent);
                        RotateLeft(grandparent);
                        current.Color = RbColor.Black;
                        grandparent.Color = RbColor.Red;
                        break;
                    }
                }
            }
            
        }
        
        Root.Color = RbColor.Black;
    }


    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        var nodeDelete = node;
        RbNode<TKey, TValue> nodeRemoved = nodeDelete;
        RbColor colorRemoved = nodeRemoved.Color;
        RbNode<TKey, TValue>? replacement;
        RbNode<TKey, TValue>? parentReplacement;

        if (nodeDelete.Left == null)
        {
            replacement = nodeDelete.Right;
            parentReplacement = nodeDelete.Parent;
            Transplant(nodeDelete, nodeDelete.Right);
        }
        else if (nodeDelete.Right == null)
        {
            replacement = nodeDelete.Left;
            parentReplacement = nodeDelete.Parent;
            Transplant(nodeDelete, replacement);
        }
        else
        {
            nodeRemoved = nodeDelete.Right;
            while (nodeRemoved.Left != null) nodeRemoved = nodeRemoved.Left;
            colorRemoved = nodeRemoved.Color;
            replacement = nodeRemoved.Right;

            if (nodeRemoved.Parent == nodeDelete) parentReplacement = nodeRemoved;
            else
            {
                parentReplacement = nodeRemoved.Parent;
                Transplant(nodeRemoved, nodeRemoved.Right);
                nodeRemoved.Right = nodeDelete.Right;   
                nodeRemoved.Right.Parent = nodeRemoved;
            }
            Transplant(nodeDelete, nodeRemoved);
            nodeRemoved.Left = nodeDelete.Left;
            nodeRemoved.Left.Parent = nodeRemoved;
            nodeRemoved.Color = nodeDelete.Color;
        }

        if (colorRemoved == RbColor.Black)
        {
            if (replacement != null && replacement.Color == RbColor.Red) replacement.Color = RbColor.Black;
            else FixAfterRemove(replacement, parentReplacement);
        }
    }

    private void FixAfterRemove(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent) 
    {
        while (parent != null && node != Root && (node == null || node.Color == RbColor.Black))
        {
            if (node == parent.Left)
            {
                RbNode<TKey, TValue>? sibling = parent.Right;
                if (sibling != null && sibling.Color == RbColor.Red)
                {
                    sibling.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }
                if (sibling == null || (sibling.Right == null || sibling.Right.Color == RbColor.Black) 
                && (sibling.Left == null || sibling.Left.Color == RbColor.Black)) 
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = parent;
                    parent = node.Parent;
                }
                else
                {
                    if (sibling.Right == null || sibling.Right.Color == RbColor.Black)
                    {
                        if (sibling.Left != null) sibling.Left.Color = RbColor.Black;
                        sibling.Color = RbColor.Red;
                        RotateRight(sibling);
                        sibling = parent.Right;
                    }
                    sibling.Color = parent.Color;
                    parent.Color = RbColor.Black;

                    if (sibling.Right != null) sibling.Right.Color = RbColor.Black;
                    RotateLeft(parent);
                    node = Root;
                }
            }
            else
            {
                RbNode<TKey, TValue>? sibling = parent.Left;
                if (sibling != null && sibling.Color == RbColor.Red)
                {
                    sibling.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                if (sibling == null || (sibling.Left == null || sibling.Left.Color == RbColor.Black) 
                && (sibling.Right == null || sibling.Right.Color == RbColor.Black)) 
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = parent;
                    parent = node.Parent;
                }
                else
                {
                    if (sibling.Left == null || sibling.Left.Color == RbColor.Black)
                    {
                        if (sibling.Right != null) sibling.Right.Color = RbColor.Black;
                        sibling.Color = RbColor.Red;
                        RotateLeft(sibling);
                        sibling = parent.Left;
                    }
                    sibling.Color = parent.Color;
                    parent.Color = RbColor.Black;

                    if (sibling.Left != null) sibling.Left.Color = RbColor.Black;
                    RotateRight(parent);
                    node = Root;
                }
            }
        }
        if (node != null) node.Color = RbColor.Black;
    }

}