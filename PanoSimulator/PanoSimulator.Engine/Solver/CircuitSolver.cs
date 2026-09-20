using PanoSimulator.Engine.Components;
using PanoSimulator.Engine.Types;

namespace PanoSimulator.Engine.Solver;

/// <summary>
/// Devre çözücü — Union-Find + sabit nokta algoritması.
///
/// GENEL AKIŞ (her Solve() çağrısında):
///   1. Tüm terminaller → düğüm indexlerine eşle
///   2. Kapalı iç kontaklar → Union-Find kenarları
///   3. Kablolar → Union-Find kenarları
///   4. Bağlantı kümelerini (net) çıkar
///   5. Besleme terminallerinden potansiyelleri yay
///   6. Her bobin için: A1 ve A2 farklı potansiyelde ve aralarında gerilim var mı?
///      → Evet: CoilEnergized = true
///      → Hayır: CoilEnergized = false
///   7. Bobin durumu değiştiyse → başa dön (max 60 iterasyon)
///   8. 60'ta sabit noktaya ulaşmadıysa → IsOscillating = true
///
/// MÜHÜRLEMENİN SIRRΙ (Kitap §3.6):
///   Çözümü her seferinde sıfırdan BAŞLATMA.
///   CoilEnergized state'i PanelState.Parts içinde korunur.
///   İterasyon önceki durumdan başlar.
///   START bırakıldığında K1'in 13-14 mühürleme kontağı hâlâ kapalı →
///   bobin enerjili kalmaya devam → motor çalışmaya devam.
/// </summary>
public class CircuitSolver
{
    private const int MaxIterations = 60;

    // Besleme kaynağı terminalleri — bunlar her zaman sabittir, panoya konmaz.
    // Trifaze Türkiye şebekesi: L1/L2/L3 arası 380V, L-N arası 220V.
    private const string SupplyL1 = "__supply_L1";
    private const string SupplyL2 = "__supply_L2";
    private const string SupplyL3 = "__supply_L3";
    private const string SupplyN  = "__supply_N";
    private const string SupplyPE = "__supply_PE";

    /// <summary>
    /// Ana çözüm metodu. PanelState'i yerinde günceller.
    /// UI bu metodu çağırır; motor tamamen UI'dan bağımsız çalışır.
    /// </summary>
    public void Solve(PanelState state)
    {
        state.NetResults.Clear();
        state.IsOscillating = false;
        state.MainBreakerTripped = false;

        // Enerji yoksa hızlı çık
        if (!state.PowerOn)
        {
            state.SolverIterations = 0;
            state.MotorPhaseCount = 0;
            state.MotorGrounded = false;
            return;
        }

        // Buton durumlarını Placement state'lerine yansıt
        ApplyButtonStates(state);

        // Termik simülasyonu yansıt
        ApplyThermalStates(state);

        // Sabit nokta döngüsü
        for (int iter = 0; iter < MaxIterations; iter++)
        {
            state.SolverIterations = iter + 1;

            var netMap = BuildNetMap(state);
            var results = ComputeNetResults(state, netMap);

            // Kısa devre kontrolü — otomat açar, çıkış
            if (FaultDetector.CheckForShortCircuit(results))
            {
                state.MainBreakerTripped = true;
                state.NetResults = results;
                // Otomat açınca tüm breaker'ları aç
                foreach (var p in state.Parts.Where(p => p.Type == "breaker"))
                    p.State.BreakerClosed = false;
                state.MotorPhaseCount = 0;
                return;
            }

            // Bobin durumlarını güncelle
            bool anyChanged = UpdateCoilStates(state, results, netMap);

            state.NetResults = results;

            // Değişme yoksa sabit noktaya ulaştık
            if (!anyChanged)
            {
                ComputeMotorState(state, results, netMap);
                return;
            }
        }

        // 60 iterasyonda sabit nokta yok → salınım (flaşör/buzzer devreleri)
        state.IsOscillating = true;
        ComputeMotorState(state, state.NetResults, BuildNetMap(state));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Terminal → Düğüm Eşleme
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tüm terminalleri (ve besleme kaynaklarını) Union-Find düğümlerine eşler.
    /// Anahtar format: "placementId:terminalId" (örn: "k1:A1", "__supply_L1")
    /// </summary>
    private Dictionary<string, int> BuildNodeIndex(PanelState state)
    {
        var index = new Dictionary<string, int>();
        int next = 0;

        // Besleme kaynakları (sabit)
        // Hem "__supply_L1" hem de "__supply:L1" formatını destekle
        // (testlerde Wire EndPoint olarak yazılırken iki format kullanılabilir)
        index[SupplyL1] = next;   // "__supply_L1"
        index["__supply:L1"] = next++;  // test formatı
        index[SupplyL2] = next;
        index["__supply:L2"] = next++;
        index[SupplyL3] = next;
        index["__supply:L3"] = next++;
        index[SupplyN] = next;
        index["__supply:N"] = next++;
        index[SupplyPE] = next;
        index["__supply:PE"] = next++;

        // Parça terminalleri
        foreach (var placement in state.Parts)
        {
            var def = ComponentCatalog.Get(placement.Type);
            if (def == null) continue;

            foreach (var terminal in def.Terminals)
            {
                string key = NodeKey(placement.Id, terminal.Id);
                if (!index.ContainsKey(key))
                    index[key] = next++;
            }
        }

        return index;
    }

    private static string NodeKey(string placementId, string terminalId)
        => $"{placementId}:{terminalId}";

    // ─────────────────────────────────────────────────────────────────────────
    // 2-3. Union-Find Ağı Kurma (İç Kontaklar + Kablolar)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tüm terminaller ve bağlantıları Union-Find ile çöz.
    /// Dön: terminal anahtar → net kümesinin kök indeksi
    /// </summary>
    private Dictionary<string, int> BuildNetMap(PanelState state)
    {
        var nodeIndex = BuildNodeIndex(state);
        var uf = new UnionFind(nodeIndex.Count);

        // Kapalı iç kontakları kenar olarak ekle (her parça için)
        foreach (var placement in state.Parts)
        {
            var def = ComponentCatalog.Get(placement.Type);
            if (def == null) continue;

            foreach (var (termA, termB) in def.InternalLinks(placement.State))
            {
                string keyA = NodeKey(placement.Id, termA);
                string keyB = NodeKey(placement.Id, termB);

                if (nodeIndex.TryGetValue(keyA, out int idxA) &&
                    nodeIndex.TryGetValue(keyB, out int idxB))
                {
                    uf.Union(idxA, idxB);
                }
            }
        }

        // Kabloları kenar olarak ekle
        foreach (var wire in state.Wires)
        {
            string keyA = NodeKey(wire.A.PlacementId, wire.A.TerminalId);
            string keyB = NodeKey(wire.B.PlacementId, wire.B.TerminalId);

            // Besleme kaynakları özel isimle tanımlı, kablo da bu ada bağlanabilir
            if (nodeIndex.TryGetValue(keyA, out int idxA) &&
                nodeIndex.TryGetValue(keyB, out int idxB))
            {
                uf.Union(idxA, idxB);
            }
        }

        // Terminal anahtarı → net kök indeksi eşlemesi
        var netMap = new Dictionary<string, int>();
        foreach (var (key, idx) in nodeIndex)
            netMap[key] = uf.Find(idx);

        return netMap;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4-5. Net Sonuçlarını Hesapla (Potansiyel Yayma)
    // ─────────────────────────────────────────────────────────────────────────

    private List<NetResult> ComputeNetResults(PanelState state, Dictionary<string, int> netMap)
    {
        // Her net kökü için potansiyel seti ve terminal listesi
        var netData = new Dictionary<int, NetResult>();

        void EnsureNet(int root)
        {
            if (!netData.ContainsKey(root))
                netData[root] = new NetResult { NetId = root.ToString() };
        }

        // Besleme potansiyellerini yay
        if (netMap.TryGetValue(SupplyL1, out int l1Net)) { EnsureNet(l1Net); netData[l1Net].Potentials.Add(Potential.L1); }
        if (netMap.TryGetValue(SupplyL2, out int l2Net)) { EnsureNet(l2Net); netData[l2Net].Potentials.Add(Potential.L2); }
        if (netMap.TryGetValue(SupplyL3, out int l3Net)) { EnsureNet(l3Net); netData[l3Net].Potentials.Add(Potential.L3); }
        if (netMap.TryGetValue(SupplyN,  out int nNet))  { EnsureNet(nNet);  netData[nNet].Potentials.Add(Potential.Neutral); }
        if (netMap.TryGetValue(SupplyPE, out int peNet)) { EnsureNet(peNet); netData[peNet].Potentials.Add(Potential.PE); }

        // Tüm terminal anahtarlarını net'lere ekle
        foreach (var (key, root) in netMap)
        {
            EnsureNet(root);
            netData[root].Terminals.Add(key);
        }

        return [.. netData.Values];
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Bobin Durumu Güncelleme
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Her kontaktör bobinini kontrol et: A1 ve A2 farklı nette VE
    /// aralarında anlamlı bir gerilim var mı? (Faz-Nötr = 220V)
    /// 
    /// Kumanda devresi 220V (L1-N arası) ile beslenir — Kitap §3.2.
    /// A1 = L1 potansiyelinde, A2 = N potansiyelinde → bobin çeker.
    /// </summary>
    private bool UpdateCoilStates(PanelState state, List<NetResult> results, Dictionary<string, int> netMap)
    {
        bool anyChanged = false;

        foreach (var placement in state.Parts.Where(p => p.Type == "contactor" || p.Type == "timer"))
        {
            string keyA1 = NodeKey(placement.Id, "A1");
            string keyA2 = NodeKey(placement.Id, "A2");

            if (!netMap.TryGetValue(keyA1, out int netA1) ||
                !netMap.TryGetValue(keyA2, out int netA2))
                continue;

            // A1 ve A2 aynı netteyse gerilim yok → bobin çekemez
            if (netA1 == netA2)
            {
                if (placement.State.CoilEnergized) { placement.State.CoilEnergized = false; anyChanged = true; }
                continue;
            }

            var potA1 = GetPotentials(results, netA1);
            var potA2 = GetPotentials(results, netA2);

            // Bobin çekme koşulu: A1 tarafında faz var, A2 tarafında nötr var
            // (220V AC — L-N arası besleme — Kitap §3.2)
            bool shouldEnergize =
                potA1.Any(p => p is Potential.L1 or Potential.L2 or Potential.L3) &&
                potA2.Contains(Potential.Neutral);

            // Veya ters bağlantı da çalışır (A1=N, A2=Faz) — bazı uygulamalarda
            bool reverseOk =
                potA2.Any(p => p is Potential.L1 or Potential.L2 or Potential.L3) &&
                potA1.Contains(Potential.Neutral);

            bool newState = shouldEnergize || reverseOk;
            if (newState != placement.State.CoilEnergized)
            {
                placement.State.CoilEnergized = newState;
                anyChanged = true;
            }
        }

        return anyChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Motor Durumu Hesaplama
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Motor terminallerini (U, V, W) analiz et.
    /// Her terminalde hangi faz var? Kaç farklı faz var?
    /// 3 farklı faz → motor çalışıyor.
    /// 1-2 faz → faz eksik, motor uğulduyor (sargı yanar — Kitap §2.7)
    /// 0 faz   → motor durmuş.
    /// </summary>
    private void ComputeMotorState(PanelState state, List<NetResult> results, Dictionary<string, int> netMap)
    {
        state.MotorPhaseCount = FaultDetector.CountMotorPhases(state, results, netMap);
        state.MotorGrounded = FaultDetector.CheckMotorGrounding(state, results, netMap);
        state.MotorConnection = FaultDetector.DetectMotorConnection(state, netMap);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Yardımcı Metodlar
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Verilen net kök indeksi için potansiyeller kümesini döner.</summary>
    private static HashSet<Potential> GetPotentials(List<NetResult> results, int netRoot)
    {
        var net = results.FirstOrDefault(r => r.NetId == netRoot.ToString());
        return net?.Potentials ?? [];
    }

    /// <summary>
    /// PanelState'deki buton durumlarını Placement.State'e yansıt.
    /// START buton ID'si "bt_start" veya "btn_start" veya tip "button-no" olan ilk parça.
    /// STOP buton ID'si "bt_stop" veya "btn_stop" veya tip "button-nc" olan ilk parça.
    /// </summary>
    private static void ApplyButtonStates(PanelState state)
    {
        // START — NO buton
        var startBtn = state.Parts.FirstOrDefault(
            p => p.Type == "button-no" &&
                 (p.Id.Contains("start", StringComparison.OrdinalIgnoreCase) || p.Id == "bt_start"));

        if (startBtn != null)
            startBtn.State.IsPressed = state.StartPressed;

        // STOP — NC buton
        var stopBtn = state.Parts.FirstOrDefault(
            p => p.Type == "button-nc" &&
                 (p.Id.Contains("stop", StringComparison.OrdinalIgnoreCase) || p.Id == "bt_stop"));

        if (stopBtn != null)
            stopBtn.State.IsPressed = state.StopPressed;
    }

    /// <summary>Termik simülasyon durumunu ilgili parçaya yansıt.</summary>
    private static void ApplyThermalStates(PanelState state)
    {
        var thermal = state.Parts.FirstOrDefault(p => p.Type == "thermalRelay");
        if (thermal != null)
            thermal.State.ThermalTripped = state.SimulatedThermalTrip;
    }
}
