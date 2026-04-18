﻿using System.Collections; // Подключает стандартные интерфейсы коллекций (например, IEnumerable, IEnumerator, ICollection).
using System.Diagnostics.CodeAnalysis; // Используется в методе TryGetValue
using TreeDataStructures.Interfaces; // находится интерфейс ITree<TKey, TValue>, который реализует класс BinarySearchTreeBase

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root; // корень дерева
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // чтобы сравнивать ключи

    public int Count { get; protected set; } // количество элементов в дереве
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode (key, value);
         if (Root == null) 
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }
        else
        {
            TNode? current = Root; // узел, на котором сейчас стоим
            while (true)
            {
                int cmp = Comparer.Compare(key, current.Key);
                if (cmp == 0) 
                {
                    current.Value = value;
                    return;
                }
                if (cmp < 0) 
                {
                    if (current.Left == null)
                    {
                        current.Left = newNode;
                        newNode.Parent = current;
                        break;
                    }
                    current = current.Left;
                }
                if (cmp > 0)
                {
                    if (current.Right == null)
                    {
                        current.Right = newNode;
                        newNode.Parent = current;
                        break;
                    }
                    current = current.Right;
                }
            }
            Count++;
            OnNodeAdded(newNode);
        }
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode (TNode node)
    {
        if (node.Left == null && node.Right == null) 
        {
            TNode? parent = node.Parent;
            Transplant (node, null);
            OnNodeRemoved(parent, null);
        }
        else if (node.Left != null && node.Right == null)
        {
            TNode? parent = node.Parent;
            TNode? left = node.Left;
            Transplant (node, node.Left);
            OnNodeRemoved(parent, left);
        }
        else if (node.Left == null && node.Right != null)
        {
            TNode? parent = node.Parent;
            TNode? right = node.Right;
            Transplant (node, node.Right);
            OnNodeRemoved(parent, right);
            
        }
        else // когда два ребенка надо найти минимальный элемент справа и поставить на место удаляемого узла
        {
            TNode current = node.Right;
            while (current.Left != null)
            {
                current = current.Left;
            }
            TNode? rebalanceStart;
            if (current != node.Right)
            {
                rebalanceStart = current.Parent;
                Transplant (current, current.Right); // удалили наш current
                current.Right = node.Right;
                current.Right.Parent = current;
            }
            else rebalanceStart = current;

            Transplant (node, current);
            current.Left = node.Left;
            current.Left.Parent = current;
            OnNodeRemoved(rebalanceStart, current);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key] // обращается, как к массиву
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    /// Хуки — это пустые виртуальные методы, которые вызываются в ключевые моменты жизни дерева. Они нужны, чтобы наследники могли «подключиться» к этим событиям и добавить свою логику, не переписывая основной код.
    protected virtual void OnNodeAdded(TNode newNode) { } // Главный смысл: Дать наследникам возможность отреагировать на добавление элемента без переписывания метода Add.
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { } // Главный смысл: Сообщить наследникам, где в дереве произошли изменения, чтобы они могли обновить свою логику (балансировку, статистику, кэш).
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x) // вращения дерева
    {
        if (x.Right == null) return;
        TNode y = x.Right;
        TNode? z = y.Left;
        y.Parent = x.Parent;
        if (x.Parent == null) { Root = y; }
        else if (x.IsRightChild) {x.Parent.Right = y; }
        else { x.Parent.Left = y; }
        y.Left = x;
        x.Parent = y;
        x.Right = z;
        if (z != null) { z.Parent = x; }
        
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null) return;
        TNode x = y.Left;
        TNode? z = x.Right;
        x.Parent = y.Parent;
        if (y.Parent == null) { Root = x; }
        else if (y.IsLeftChild) {y.Parent.Left = x; }
        else { y.Parent.Right = x; }
        x.Right = y;
        y.Parent = x;
        y.Left = z;
        if (z != null) { z.Parent = y; }
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateLeft (x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateRight (y.Left!);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateRight(x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateLeft(y.Left!);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrder);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrder);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrder);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    } 
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        private readonly TNode? _root;
        private TNode? _current;
        private bool _started;


        public TreeIterator(TNode? root, TraversalStrategy strategy) // констурктор
        {
            _root = root;
            _strategy = strategy;
            _current = null;
            _started = false;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => _current == null ? throw new InvalidOperationException()
        : new TreeEntry<TKey, TValue>(_current.Key, _current.Value, _current.Height);
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder) // ДА
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        while (_current.Left != null) _current = _current.Left;
                        _started = true;
                        return true;
                    }
                    return false;
                }
                
                if (_current == null) return false;

                if (_current.Right != null)
                {
                    _current = _current.Right;
                    while (_current.Left != null) _current = _current.Left;
                    return true;
                }
                TNode? child = _current;
                TNode? parent = _current.Parent;

                while (parent != null && parent.Right == child)
                {
                    child = parent;
                    parent = parent.Parent;
                }

                _current = parent;
                return _current != null;
            }

            if (_strategy == TraversalStrategy.PreOrder) // корень - лево - право ДА
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        _started = true;
                        return true;
                    }
                    return false;
                }

                if (_current == null) return false;

                if (_current.Left != null)
                {
                    _current = _current.Left;
                    return true;
                }
                if (_current.Right != null)
                {
                    _current = _current.Right;
                    return true;
                }

                TNode? child = _current;
                TNode? parent = _current.Parent;
                while (parent != null && (child == parent.Right || parent.Right == null))
                {
                    child = parent;
                    parent = parent.Parent;
                }

                if (parent == null)
                {
                    _current = null;
                    return false;
                }
                _current = parent.Right;
                return true;
            }

            if (_strategy == TraversalStrategy.PostOrder) // лево право корень ДА
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        while (_current.Left != null || _current.Right != null)
                        {
                            if (_current.Left != null) _current = _current.Left;
                            else _current = _current.Right;
                        }
                        _started = true;
                        return true;
                    }
                    return false;
                }

                if (_current == null) return false;

                TNode? parent = _current.Parent;

                if (parent == null)
                {
                    _current = null;
                    return false;
                }

                if (_current == parent.Left && parent.Right != null)
                {
                    _current = parent.Right;
                    while (_current.Left != null || _current.Right != null)
                    {
                        if (_current.Left != null) _current = _current.Left;
                        else _current = _current.Right;
                    }
                    return true;
                }
                else
                {
                    _current = parent;
                    return true;
                }
            }

            if (_strategy == TraversalStrategy.InOrderReverse) // ДА
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        while (_current.Right != null) _current = _current.Right;
                        _started = true;
                        return true;
                    }
                    return false;
                }

                if (_current == null) return false;

                if (_current.Left != null)
                {
                    _current = _current.Left;
                    while (_current.Right != null) _current = _current.Right;
                    return true;
                }

                TNode? child = _current;
                TNode? parent = _current.Parent;

                while (parent != null && parent.Left == child)
                {
                    child = parent;
                    parent = parent.Parent;
                }
                _current = parent;
                return _current != null;
            }


            if (_strategy == TraversalStrategy.PreOrderReverse) // право - лево - корень PROBLEM
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        while (_current.Left != null || _current.Right != null)
                        {
                            if (_current.Right != null) _current = _current.Right;
                            else _current = _current.Left;
                        }
                        _started = true;
                        return true;
                    }
                    return false;
                }

                if (_current == null) return false;

                TNode? parent = _current.Parent;

                if (parent == null)
                {
                    _current = null;
                    return false;
                }

                if (_current == parent.Right && parent.Left != null)
                {
                    _current = parent.Left;
                    while (_current.Right != null || _current.Left != null)
                    {
                        if (_current.Right != null) _current = _current.Right;
                        else _current = _current.Left;
                    }
                    return true;
                }
                else
                {
                    _current = parent;
                    return true;
                }
            }


            if (_strategy == TraversalStrategy.PostOrderReverse) // корень - право - лево
            {
                if (!_started)
                {
                    if (_root != null)
                    {
                        _current = _root;
                        _started = true;
                        return true;
                    }
                    return false;
                }

                if (_current == null) return false;

                if (_current.Right != null)
                {
                    _current = _current.Right;
                    return true;
                }
                if (_current.Left != null)
                {
                    _current = _current.Left;
                    return true;
                }

                TNode? child = _current;
                TNode? parent = _current.Parent;
                while (parent != null && (child == parent.Left || parent.Left == null))
                {
                    child = parent;
                    parent = parent.Parent;
                }

                if (parent == null)
                {
                    _current = null;
                    return false;
                }
                _current = parent.Left;
                return true;
            }


            throw new NotImplementedException("Strategy not implemented");
        }
        
        public void Reset()
        {
            _current = null;
            _started = false;
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}