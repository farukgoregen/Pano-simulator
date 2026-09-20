namespace PanoSimulator.Engine.Types;

/// <summary>
/// Bir trifaze sistemdeki potansiyel (gerilim seviyesi).
/// Solver, her Union-Find kümesine (net'e) bir potansiyel atar.
///
/// Türkiye şebekesi: L1/L2/L3 arası 380V (faz-faz), L-N arası 220V (faz-nötr).
/// Kumanda devresi genellikle L1-N (220V AC) arasından beslenir (Kitap §3.2).
/// </summary>
public enum Potential
{
    /// <summary>Belirsiz / bağlantısız net</summary>
    Unknown,

    /// <summary>Faz 1 (R fazı) — Güç devresi</summary>
    L1,

    /// <summary>Faz 2 (S fazı) — Güç devresi</summary>
    L2,

    /// <summary>Faz 3 (T fazı) — Güç devresi</summary>
    L3,

    /// <summary>Nötr — Kumanda devresi dönüş hattı</summary>
    Neutral,

    /// <summary>
    /// Koruma iletkeni (toprak/PE).
    /// Motor gövdesine, pano kasasına bağlanır — IEC 60204-1.
    /// Bir nette L+PE bir arada ise: elektrik kaçağı → uyarı/arıza.
    /// </summary>
    PE
}

/// <summary>
/// Bir Union-Find kümesinin (net'in) çözüm sonucu.
/// Solver, her net için bunu doldurur ve arıza tespitinde kullanır.
/// </summary>
public class NetResult
{
    public string NetId { get; set; } = string.Empty;

    /// <summary>Bu nette hangi potansiyeller var?</summary>
    public HashSet<Potential> Potentials { get; set; } = [];

    /// <summary>Bu nette bulunan tüm terminal Id'leri</summary>
    public HashSet<string> Terminals { get; set; } = [];

    /// <summary>
    /// Kısa devre var mı?
    /// Koşullar: iki farklı faz aynı nette, VEYA faz+nötr arada yük yok.
    /// Kitap §1.1: "Kısa devrede akım çok büyür, sigorta/otomat açar."
    /// </summary>
    public bool HasShortCircuit => Potentials.Count(p => p is Potential.L1 or Potential.L2 or Potential.L3) > 1
                                   || (Potentials.Contains(Potential.L1) && Potentials.Contains(Potential.Neutral))
                                   || (Potentials.Contains(Potential.L2) && Potentials.Contains(Potential.Neutral))
                                   || (Potentials.Contains(Potential.L3) && Potentials.Contains(Potential.Neutral));

    /// <summary>
    /// Elektrik kaçağı var mı? (Faz + PE aynı nette)
    /// Kitap §2.6: Motor koruma şalteri kaçak akımı da keser.
    /// </summary>
    public bool HasGroundFault => Potentials.Contains(Potential.PE)
                                  && (Potentials.Contains(Potential.L1)
                                      || Potentials.Contains(Potential.L2)
                                      || Potentials.Contains(Potential.L3));
}

/// <summary>
/// Panodaki tüm anlık durum — solver'ın girdi ve çıktısı.
/// Bu nesne her "tick"te solver tarafından güncellenir.
/// </summary>
public class PanelState
{
    // ─── Kullanıcının kurduğu devre ───────────────────────────────────────────

    /// <summary>Panoya yerleştirilen tüm parçalar</summary>
    public List<Placement> Parts { get; set; } = [];

    /// <summary>Çekilen tüm kablolar</summary>
    public List<Wire> Wires { get; set; } = [];

    // ─── Kullanıcı komutları ──────────────────────────────────────────────────

    /// <summary>Ana otomat / güç verildi mi?</summary>
    public bool PowerOn { get; set; }

    /// <summary>START butonu basılı mı? (pointerdown/pointerup)</summary>
    public bool StartPressed { get; set; }

    /// <summary>STOP butonu basılı mı?</summary>
    public bool StopPressed { get; set; }

    /// <summary>
    /// "Termik attır" simülasyonu — kullanıcı manüel tetikler.
    /// Gerçekte: aşırı akım bimetal elemanı ısıtır ve kontak değiştirir.
    /// Kitap §2.5.
    /// </summary>
    public bool SimulatedThermalTrip { get; set; }

    // ─── Solver çıktısı ───────────────────────────────────────────────────────

    /// <summary>
    /// Son çözüm döngüsündeki net sonuçları.
    /// UI durum ekranı bu listeden mesajları üretir.
    /// </summary>
    public List<NetResult> NetResults { get; set; } = [];

    /// <summary>Solver kaç iterasyonda kararlı duruma ulaştı? (60'ta takılırsa salınım)</summary>
    public int SolverIterations { get; set; }

    /// <summary>Devre salınımda mı? (flaşör/buzzer devreleri bu durumu kullanır)</summary>
    public bool IsOscillating { get; set; }

    /// <summary>Kısa devre nedeniyle besleme otomatı açtı mı?</summary>
    public bool MainBreakerTripped { get; set; }

    // ─── Motor durumu ─────────────────────────────────────────────────────────

    /// <summary>
    /// Motora gelen faz sayısı (0, 1, 2 veya 3).
    /// 3 → motor sorunsuz çalışıyor.
    /// 1-2 → faz eksik — motor uğulduyor, sargı yanar (Kitap §2.7).
    /// 0 → motor duruyor.
    /// </summary>
    public int MotorPhaseCount { get; set; }

    /// <summary>Motor PE hattına bağlı mı? (IEC 60204-1 zorunluluğu)</summary>
    public bool MotorGrounded { get; set; }

    /// <summary>Motorun Yıldız/Üçgen bağlantı durumu</summary>
    public string MotorConnection { get; set; } = "";
}
