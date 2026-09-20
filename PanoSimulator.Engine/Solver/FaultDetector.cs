using PanoSimulator.Engine.Types;

namespace PanoSimulator.Engine.Solver;

/// <summary>
/// Devredeki elektriksel arızaları (Kısa Devre, Kaçak, Faz Eksikliği vb.) tespit eden sınıf.
/// CircuitSolver tarafından her iterasyonda veya çözüm sonunda çağrılır.
/// </summary>
public static class FaultDetector
{
    /// <summary>
    /// Ağlar (NetResults) üzerinde kısa devre olup olmadığını kontrol eder.
    /// Kısa devre: L1-L2, L1-L3, L2-L3, L1-N gibi iki farklı güç kaynağının aynı net (düğüm)
    /// üzerinde buluşmasıdır.
    /// </summary>
    public static bool CheckForShortCircuit(IEnumerable<NetResult> results)
    {
        return results.Any(r => r.HasShortCircuit);
    }

    /// <summary>
    /// Ağlar üzerinde Toprak Kaçağı (PE) olup olmadığını kontrol eder.
    /// Kaçak: L1, L2, L3 veya Nötr hattının PE (Koruma Topraklaması) ile aynı net üzerinde buluşmasıdır.
    /// </summary>
    public static bool CheckForGroundFault(IEnumerable<NetResult> results)
    {
        return results.Any(r => r.HasGroundFault);
    }

    /// <summary>
    /// Motorun terminallerine (U1, V1, W1) gelen potansiyellere bakarak motora kaç farklı
    /// faz ulaştığını hesaplar. Yıldız-Üçgen motor (6 uçlu)
    /// </summary>
    public static int CountMotorPhases(PanelState state, List<NetResult> results, Dictionary<string, int> netMap)
    {
        var motor = state.Parts.FirstOrDefault(p => p.Type == "motor");
        if (motor == null) return 0;

        var phases = new HashSet<Potential>();
        foreach (var term in new[] { "U1", "V1", "W1" })
        {
            string key = $"{motor.Id}:{term}";
            if (!netMap.TryGetValue(key, out int netId)) continue;
            var net = results.FirstOrDefault(r => r.NetId == netId.ToString());
            if (net == null) continue;

            foreach (var p in net.Potentials.Where(p => p is Potential.L1 or Potential.L2 or Potential.L3))
            {
                phases.Add(p);
            }
        }
        return phases.Count;
    }

    /// <summary>
    /// Motorun sargı bağlantısının Yıldız (Star) mı yoksa Üçgen (Delta) mı olduğunu tespit eder.
    /// Yıldız: W2, U2, V2 aynı net üzerinde (kısa devre)
    /// Üçgen: U1-W2 aynı net, V1-U2 aynı net, W1-V2 aynı net.
    /// </summary>
    public static string DetectMotorConnection(PanelState state, Dictionary<string, int> netMap)
    {
        var motor = state.Parts.FirstOrDefault(p => p.Type == "motor");
        if (motor == null) return "";

        string mId = motor.Id;
        bool hasU1 = netMap.TryGetValue($"{mId}:U1", out int u1);
        bool hasV1 = netMap.TryGetValue($"{mId}:V1", out int v1);
        bool hasW1 = netMap.TryGetValue($"{mId}:W1", out int w1);
        bool hasW2 = netMap.TryGetValue($"{mId}:W2", out int w2);
        bool hasU2 = netMap.TryGetValue($"{mId}:U2", out int u2);
        bool hasV2 = netMap.TryGetValue($"{mId}:V2", out int v2);

        // Yıldız (Star) Kontrolü: W2, U2, V2 aynı net mi? Ve u1, v1, w1'den farklı olmalılar (kısa devre olmamalı)
        if (hasW2 && hasU2 && hasV2)
        {
            if (w2 == u2 && u2 == v2)
            {
                // Sargıların giriş uçlarından farklı bir net mi? 
                // Eğer U1=W2 ise ve Yıldız yapılmışsa zaten sistem kısa devredir.
                return "Star";
            }
        }

        // Üçgen (Delta) Kontrolü: U1=W2, V1=U2, W1=V2
        if (hasU1 && hasW2 && hasV1 && hasU2 && hasW1 && hasV2)
        {
            if (u1 == w2 && v1 == u2 && w1 == v2)
            {
                return "Delta";
            }
        }

        return ""; // Geçerli bir sargı bağlantısı yok
    }

    /// <summary>
    /// Motor gövdesinin PE terminali üzerinden topraklanıp topraklanmadığını kontrol eder.
    /// </summary>
    public static bool CheckMotorGrounding(PanelState state, List<NetResult> results, Dictionary<string, int> netMap)
    {
        var motor = state.Parts.FirstOrDefault(p => p.Type == "motor");
        if (motor == null) return true;

        if (netMap.TryGetValue($"{motor.Id}:PE", out int netId))
        {
            var net = results.FirstOrDefault(r => r.NetId == netId.ToString());
            return net != null && net.Potentials.Contains(Potential.PE);
        }
        return false;
    }
}
