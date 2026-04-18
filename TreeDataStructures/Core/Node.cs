﻿﻿namespace TreeDataStructures.Core;

public class Node<TKey, TValue, TNode>(TKey key, TValue value) where TNode : Node<TKey, TValue, TNode> 
{
    public TKey Key { get; set; } = key;
    public TValue Value { get; set; } = value;
    
    public TNode? Left { get; set; }
    public TNode? Right { get; set; }
    public TNode? Parent { get; set; }
    
    public bool IsLeftChild  => this.Parent != null && this.Parent.Left == this;
    public bool IsRightChild => this.Parent != null && this.Parent.Right == this;

    public int Height => 1 + Math.Max(Left?.Height ?? 0, Right?.Height ?? 0);
}