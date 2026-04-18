﻿using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{

    private int GetHeight (AvlNode<TKey, TValue>? node)
    {
        if (node == null) return 0;
        else return node.Height;
    }

    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = Math.Max(GetHeight(node.Left), GetHeight(node.Right)) + 1;
    }

    private int GetBalance(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return 0;
        return GetHeight(node.Left) - GetHeight(node.Right);
    }

    private void RotateLeftAvl(AvlNode<TKey, TValue> node)
    {
        RotateLeft(node);
        UpdateHeight(node);
        if (node.Parent != null) UpdateHeight(node.Parent);
    }

    private void RotateRightAvl(AvlNode<TKey, TValue> node)
    {
        RotateRight(node);
        UpdateHeight(node);
        if (node.Parent != null) UpdateHeight(node.Parent);
    }


    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode.Parent;
        while (current != null)
        {
            UpdateHeight(current);
            int balance = GetBalance(current);
            if (balance > 1 || balance < -1)
            {
                if (balance > 1) // слева первес
                {
                    int balanceLeftChild = GetBalance(current.Left);
                    if (balanceLeftChild >= 0) RotateRightAvl(current); //LL
                    else // LR
                    {
                        RotateLeftAvl(current.Left!);
                        RotateRightAvl(current);
                    }
                }
                else // справа
                {
                    int balanceRightChild = GetBalance(current.Right);
                    if (balanceRightChild <= 0) RotateLeftAvl(current); //RR
                    else //RL
                    {
                        RotateRightAvl(current.Right!);
                        RotateLeftAvl(current);
                    }
                }
                break;
            }
            current = current.Parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child) // у кого изменился child, на что изменился
    {
        var current = parent;

        while (current != null)
        {
            UpdateHeight(current);
            int balance = GetBalance(current);

            AvlNode<TKey, TValue> next = current.Parent;

            if (balance > 1 || balance < -1)
            {
                if (balance > 1)
                {
                    if (GetBalance(current.Left) >= 0) RotateRightAvl(current);
                    else 
                    {
                        RotateLeftAvl(current.Left!);
                        RotateRightAvl(current);
                    }
                }
                else
                {
                    if (GetBalance(current.Right) <= 0) RotateLeftAvl(current);
                    else
                    {
                        RotateRightAvl(current.Right!);
                        RotateLeftAvl(current);
                    }
                }
            }
            current = next;
        }
    }

    
}