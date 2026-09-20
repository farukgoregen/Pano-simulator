namespace PanoSimulator.Engine.Solver;

/// <summary>
/// Union-Find (Disjoint Set Union) veri yapısı.
/// 
/// NE YAPIR?
/// Düğümleri (node) kümeler halinde gruplar.
/// "Bu iki düğüm aynı kümede mi?" sorusunu O(α) ≈ O(1) sürede cevaplar.
///
/// NEDEN KULLANIYORUZ?
/// Devre analizi için: Kablolar ve kapalı kontaklar terminalleri birbirine bağlar.
/// Bağlı terminaller aynı "net"i (ağı) oluşturur — aynı potansiyeldeler.
/// Union-Find bu ağları çok verimli bulur.
///
/// ÖRNEK:
/// Q1.1 — kablo — K1.1   →  Union(Q1.1, K1.1)
/// K1.1 — kontak — K1.2  →  Union(K1.1, K1.2)
/// Find(Q1.1) == Find(K1.2)  →  TRUE (aynı ağdalar)
///
/// YÖNTEM:
/// "Path compression" + "Union by rank" ile optimize edilmiş.
/// Kaynak: Sedgewick - Algorithms (Union-Find chapter)
/// </summary>
public class UnionFind
{
    // Her düğümün "ebeveyn" göstericisi. parent[i]==i ise bu köktür.
    private readonly int[] _parent;

    // Ağaç dengeleme için rank (yaklaşık derinlik)
    private readonly int[] _rank;

    // Düğüm sayısı
    public int Count { get; }

    public UnionFind(int count)
    {
        Count = count;
        _parent = new int[count];
        _rank = new int[count];

        // Başlangıçta her düğüm kendi kümesinin kökü
        for (int i = 0; i < count; i++)
            _parent[i] = i;
    }

    /// <summary>
    /// i düğümünün ait olduğu kümenin kök temsilcisini döner.
    /// Path compression: her seferinde kökü kısaltır → gelecekte daha hızlı.
    /// </summary>
    public int Find(int i)
    {
        if (_parent[i] != i)
            _parent[i] = Find(_parent[i]); // Path compression
        return _parent[i];
    }

    /// <summary>
    /// i ve j'nin kümelerini birleştirir (kablo veya kapalı kontak bağlantısı).
    /// Union by rank: daha kısa ağacı, daha uzun ağacın altına bağlar.
    /// Zaten aynı kümedeyse false döner (döngü tespiti için kullanışlı).
    /// </summary>
    public bool Union(int i, int j)
    {
        int rootI = Find(i);
        int rootJ = Find(j);

        if (rootI == rootJ) return false; // Zaten aynı kümede

        // Küçük ağacı büyük ağacın altına bağla (dengeleme)
        if (_rank[rootI] < _rank[rootJ])
            _parent[rootI] = rootJ;
        else if (_rank[rootI] > _rank[rootJ])
            _parent[rootJ] = rootI;
        else
        {
            _parent[rootJ] = rootI;
            _rank[rootI]++;
        }

        return true;
    }

    /// <summary>
    /// i ve j aynı kümede mi? (Aynı elektrik ağında mı?)
    /// </summary>
    public bool Connected(int i, int j)
        => Find(i) == Find(j);

    /// <summary>
    /// Kaç ayrı küme var? (Her küme bir elektrik ağı)
    /// </summary>
    public int ComponentCount()
    {
        var roots = new HashSet<int>();
        for (int i = 0; i < Count; i++)
            roots.Add(Find(i));
        return roots.Count;
    }

    /// <summary>
    /// Kök temsilcisine göre gruplandırılmış kümeleri döner.
    /// Solver'ın "her net için terminalleri bul" adımında kullanılır.
    /// </summary>
    public Dictionary<int, List<int>> GetComponents()
    {
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < Count; i++)
        {
            int root = Find(i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = [];
                groups[root] = list;
            }
            list.Add(i);
        }
        return groups;
    }
}
