namespace SmartX.API.Part3;

/// <summary>
/// Self-balancing binary search tree (AVL). After every insert or delete the heights of the two
/// subtrees of any node differ by at most 1, so Insert, lookup and RemoveMin are O(log n) and a
/// range query visits only the branches that can contain matching keys.
/// </summary>
public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private sealed class Node
    {
        public TKey Key;
        public TValue Value;
        public Node? Left;
        public Node? Right;
        public int Height = 1;
        public Node(TKey key, TValue value) { Key = key; Value = value; }
    }

    private Node? _root;

    public int Count { get; private set; }
    public int Height => H(_root);

    public void Insert(TKey key, TValue value) => _root = Insert(_root, key, value);

    public bool TryGetValue(TKey key, out TValue? value)
    {
        var n = _root;
        while (n != null)
        {
            int c = key.CompareTo(n.Key);
            if (c == 0) { value = n.Value; return true; }
            n = c < 0 ? n.Left : n.Right;
        }
        value = default;
        return false;
    }

    /// <summary>Removes the smallest (oldest) key. Used to cap how much history a node keeps.</summary>
    public bool RemoveMin(out TKey key)
    {
        if (_root == null) { key = default!; return false; }
        _root = RemoveMin(_root, out var min);
        key = min.Key;
        Count--;
        return true;
    }

    /// <summary>
    /// All entries with from &lt;= key &lt;= to, in key order. Subtrees that cannot contain a match
    /// are skipped, and 'visited' reports how many nodes were actually touched.
    /// </summary>
    public List<KeyValuePair<TKey, TValue>> Range(TKey from, TKey to, out int visited)
    {
        var result = new List<KeyValuePair<TKey, TValue>>();
        int v = 0;
        Collect(_root, from, to, result, ref v);
        visited = v;
        return result;
    }

    /// <summary>The newest 'take' values, oldest first (reverse in-order walk).</summary>
    public List<TValue> Latest(int take)
    {
        var acc = new List<TValue>();
        Newest(_root, take, acc);
        acc.Reverse();
        return acc;
    }

    // ---------------------------------------------------------------- internals

    private static int H(Node? n) => n?.Height ?? 0;
    private static void Update(Node n) => n.Height = 1 + Math.Max(H(n.Left), H(n.Right));
    private static int Balance(Node n) => H(n.Left) - H(n.Right);

    private static Node RotateRight(Node y)
    {
        var x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        Update(y);
        Update(x);
        return x;
    }

    private static Node RotateLeft(Node x)
    {
        var y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        Update(x);
        Update(y);
        return y;
    }

    private static Node Rebalance(Node n)
    {
        Update(n);
        int b = Balance(n);
        if (b > 1)
        {
            if (Balance(n.Left!) < 0) n.Left = RotateLeft(n.Left!);      // left-right case
            return RotateRight(n);                                       // left-left case
        }
        if (b < -1)
        {
            if (Balance(n.Right!) > 0) n.Right = RotateRight(n.Right!);  // right-left case
            return RotateLeft(n);                                        // right-right case
        }
        return n;
    }

    private Node Insert(Node? n, TKey key, TValue value)
    {
        if (n == null) { Count++; return new Node(key, value); }
        int c = key.CompareTo(n.Key);
        if (c < 0) n.Left = Insert(n.Left, key, value);
        else if (c > 0) n.Right = Insert(n.Right, key, value);
        else { n.Value = value; return n; }
        return Rebalance(n);
    }

    private static Node? RemoveMin(Node n, out Node min)
    {
        if (n.Left == null) { min = n; return n.Right; }
        n.Left = RemoveMin(n.Left, out min);
        return Rebalance(n);
    }

    private static void Collect(Node? n, TKey from, TKey to, List<KeyValuePair<TKey, TValue>> acc, ref int visited)
    {
        if (n == null) return;
        visited++;
        if (from.CompareTo(n.Key) < 0) Collect(n.Left, from, to, acc, ref visited);      // left may hold matches
        if (from.CompareTo(n.Key) <= 0 && to.CompareTo(n.Key) >= 0) acc.Add(new(n.Key, n.Value));
        if (to.CompareTo(n.Key) > 0) Collect(n.Right, from, to, acc, ref visited);       // right may hold matches
    }

    private static void Newest(Node? n, int take, List<TValue> acc)
    {
        if (n == null || acc.Count >= take) return;
        Newest(n.Right, take, acc);
        if (acc.Count < take) acc.Add(n.Value);
        Newest(n.Left, take, acc);
    }
}