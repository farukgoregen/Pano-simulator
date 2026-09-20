namespace PanoSimulator.Engine.Types;

/// <summary>
/// Bir parçanın o anki çalışma durumu.
/// Solver bu state'i okuyarak hangi iç kontakların kapalı olduğunu hesaplar.
/// </summary>
public class PartState
{
    /// <summary>
    /// Kontaktör bobini çekti mi?
    /// Çekince: NO kontakları (1-2, 3-4, 5-6, 13-14) KAPANIR.
    /// NC kontakları (21-22 vb.) açılır — ters kilitleme için kullanılır.
    /// Kitap §2.2: "Bobin enerjilendikten sonra çekme kuvveti oluşur..."
    /// </summary>
    public bool CoilEnergized { get; set; }

    /// <summary>
    /// Termik röle atmış mı?
    /// Atınca: NC kontak 95-96 AÇILIR → kumanda devresi kesilir → motor durur.
    /// NO kontak 97-98 KAPANIR → arıza lambası yakılabilir.
    /// Kitap §2.5: "Bimetal eleman aşırı ısınınca kontakları değiştirir."
    /// </summary>
    public bool ThermalTripped { get; set; }

    /// <summary>
    /// Buton şu an basılı mı? (Pointer Events: pointerdown=true, pointerup=false)
    /// START butonu NO (Normally Open): basılıysa kapanır.
    /// STOP butonu NC (Normally Closed): basılıysa açılır.
    /// </summary>
    public bool IsPressed { get; set; }

    /// <summary>
    /// Butonun tipi: true = NO (Normalde Açık), false = NC (Normalde Kapalı).
    /// Kitap §2.9: NO ve NC buton farkı.
    /// </summary>
    public bool IsNormallyOpen { get; set; } = true;

    /// <summary>
    /// Otomat/şalter kapalı mı? (Kısa devrede solver bunu false yapar.)
    /// </summary>
    public bool BreakerClosed { get; set; } = true;

    /// <summary>
    /// Zaman rölesi için geçen süre (ms). Faz 7'de kullanılacak.
    /// </summary>
    public double TimerElapsedMs { get; set; }

    /// <summary>
    /// Zaman rölesi ayar süresi (ms). Faz 7'de kullanılacak.
    /// </summary>
    public double TimerSetpointMs { get; set; }
}
