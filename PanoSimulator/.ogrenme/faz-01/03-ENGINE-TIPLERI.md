# 03 — Engine Tipleri: Her Dosya Ne İşe Yarıyor?

Bu klasördeki her dosya, elektrik panosundaki bir kavramı C# ile ifade ediyor.
Önce elektrik kavramını anla, sonra kodun neden öyle yazıldığını anlarsın.

---

## `Terminal.cs` — Bağlantı Noktası

### Elektrik nedir?
Her elektrik cihazının "bacakları" var. Kontaktörün A1, A2 (bobin uçları),
1, 2, 3, 4, 5, 6 (güç kontakları), 13, 14 (yardımcı kontak) terminalleri var.
Bu numaralar IEC standardına göre belirlenmiş — dünya genelinde aynı.

### Kod:
```csharp
public record Terminal(string Id, double X, double Y);
```

| Alan | Anlamı |
|------|--------|
| `Id` | Terminal numarası: `"A1"`, `"13"`, `"95"` gibi |
| `X` | SVG'de bu terminalin yatay konumu (piksel) |
| `Y` | SVG'de bu terminalin dikey konumu (piksel) |

### Neden `record`?
Terminal değişmiyor. Kontaktörün A1 terminali her zaman A1'dir.
`record` buna uygun — "bir kez tanımla, değiştirme."

### Örnekler:
```csharp
var A1 = new Terminal("A1", 10, 0);   // Bobin besleme ucu
var A2 = new Terminal("A2", 10, 80);  // Bobin dönüş ucu
var t13 = new Terminal("13", 60, 0);  // Yardımcı NO kontak giriş
var t14 = new Terminal("14", 60, 80); // Yardımcı NO kontak çıkış
```

---

## `Wire.cs` — Kablo

### Elektrik nedir?
İki terminal arasındaki iletken. Rengi IEC 60204-1 standardına göre seçilmeli.
Yanlış renk = uyarı (seviye kuralında denetlenecek).

### Kod — 3 parça:

**1. `WireColor` enum — Kablo Renkleri:**
```csharp
public enum WireColor
{
    Black,       // Siyah   → AC ve DC güç devresi
    Red,         // Kırmızı → AC kumanda devresi
    DarkBlue,    // Koyu Mavi → DC kumanda devresi
    LightBlue,   // Açık Mavi → Nötr iletkeni
    YellowGreen, // Sarı-Yeşil → PE (toprak) — BAŞKA AMAÇLA KULLANMA!
    Orange       // Turuncu → Harici kilitleme devresi
}
```

**2. `EndPoint` record — Kablo Ucu:**
```csharp
public record EndPoint(string PlacementId, string TerminalId);
// PlacementId: Hangi parça? "k1" (K1 kontaktörü)
// TerminalId: Hangi terminal? "13" (yardımcı NO kontak giriş)
```

**3. `Wire` record — Kablo:**
```csharp
public record Wire(EndPoint A, EndPoint B, WireColor Color, double CrossSectionMm2 = 1.5);
```
`CrossSectionMm2` = Kablo kesiti (mm²). Varsayılan 1.5mm² — kumanda devreleri için yeterli.
Motor güç kablosu için daha büyük kesit gerekir (Faz 8'de kural olarak denetlenecek).

### Kullanım örneği:
```csharp
// K1 mühürleme kontağı (13-14) — START butonuna paralel bağlı
// Kablo rengi kırmızı çünkü kumanda devresi (IEC 60204-1)
var muhurlemKablosu = new Wire(
    A: new EndPoint("k1", "13"),    // K1 kontaktörünün 13. terminali
    B: new EndPoint("bt_start", "3"), // START butonunun 3. terminali
    Color: WireColor.Red            // AC kumanda → kırmızı
);
```

---

## `PartState.cs` — Parçanın Anlık Durumu

### Elektrik nedir?
Bir kontaktör bazen çekmiş (bobin enerjili), bazen düşmüş olur.
Termik röle bazen atmış, bazen normal konumda olur.
Bu "an'daki durum" değişkendir — her solver tick'inde güncellenebilir.

### Kod:
```csharp
public class PartState
{
    public bool CoilEnergized { get; set; }    // Kontaktör bobini çekti mi?
    public bool ThermalTripped { get; set; }   // Termik röle atmış mı?
    public bool IsPressed { get; set; }        // Buton şu an basılı mı?
    public bool IsNormallyOpen { get; set; }   // Buton tipi: NO mu NC mi?
    public bool BreakerClosed { get; set; }    // Otomat/şalter kapalı mı?
    public double TimerElapsedMs { get; set; } // Zaman rölesi sayacı (Faz 7)
    public double TimerSetpointMs { get; set; } // Zaman rölesi ayarı (Faz 7)
}
```

### Neden `class` (record değil)?
Durum sürekli değişiyor. Bobin bir an çekiyor, bir an düşüyor.
`class` → içeriği değiştirilebilir. `record` → değişmez. Doğru seçim `class`.

### Her alanın elektrik anlamı:

| Alan | true iken | false iken |
|------|-----------|------------|
| `CoilEnergized` | Kontaktör çekmiş → NO kontaklar kapalı | Düşmüş → NO açık, NC kapalı |
| `ThermalTripped` | Termik atmış → 95-96 (NC) açık, 97-98 (NO) kapandı | Normal → 95-96 kapalı |
| `IsPressed` | Buton basılı | Serbest |
| `IsNormallyOpen` | NO buton (START) | NC buton (STOP) |
| `BreakerClosed` | Otomat kapalı, geçiriyor | Açık, kesti (kısa devrede solver bunu false yapar) |

---

## `PartDefinition.cs` — Malzeme Tanımı (Katalog Girdisi)

### Nedir?
"Kontaktör nasıl bir şeydir?" sorusunun cevabı.
Terminal listesi ve "bobin çekince hangi kontaklar kapanır?" mantığı burada.

### Kod:
```csharp
public record PartDefinition(
    string Type,          // "contactor", "thermalRelay", "button-no"...
    string Label,         // "K1 Kontaktör" (kullanıcıya gösterilen)
    string Description,   // Kısa açıklama (çekmecede)
    IReadOnlyList<Terminal> Terminals,  // Terminal listesi
    Func<PartState, IEnumerable<(string TermA, string TermB)>> InternalLinks,
    int WidthUnits = 4,   // DIN ray genişliği (1 birim = 18mm)
    PartCategory Category = PartCategory.Protection
);
```

**`InternalLinks`** en kritik alan. Solver bunu her tick'te çağırır:
```csharp
// Faz 2'de kontaktör için şöyle yazılacak:
InternalLinks: (state) =>
{
    if (state.CoilEnergized)
        // Bobin çekti → güç ve yardımcı NO kontaklar kapandı
        return new[] { ("1","2"), ("3","4"), ("5","6"), ("13","14") };
    else
        // Bobin düşük → hiç NO kontak kapalı değil
        return Array.Empty<(string,string)>();
}
```

### `PartCategory` enum:
```csharp
public enum PartCategory
{
    Protection,  // Otomat, sigorta, termik
    Switching,   // Kontaktör
    Relay,       // Zaman rölesi
    PushButton,  // Butonlar
    Indicator,   // Sinyal lambaları
    Field,       // Motor (pano dışı)
    Terminal     // Klemens
}
```
Bu kategoriler malzeme çekmecesinde gruplamak için kullanılacak (Faz 4).

---

## `Placement.cs` — Panoya Yerleştirilen Parça

### Fark nedir? PartDefinition vs Placement?

```
PartDefinition = Şablonun kendisi (katalogdaki)
    "Kontaktör denen şey şöyle terminallere sahip..."

Placement = O şablondan bir örnek (panodaki)
    "K1 isimli kontaktörü panoyu şu koordinata koydum"
```

Aynı seviyede iki kontaktör (K1 ve K2) kullanılabilir →
İki ayrı `Placement`, ama aynı `PartDefinition`.

### Kod:
```csharp
public class Placement
{
    public string Id { get; set; }        // "k1", "f2", "bt_start"
    public string Type { get; set; }      // "contactor" → katalogdan çekilir
    public double X { get; set; }         // SVG'deki X konumu
    public double Y { get; set; }         // SVG'deki Y konumu
    public int RailIndex { get; set; }    // Hangi DIN rayna yapışık? (0,1,2 veya -1=saha)
    public PartState State { get; set; }  // Bu parçanın anlık durumu
    public string UserLabel { get; set; } // Kullanıcının verdiği etiket
}
```

---

## `PanelState.cs` — Tüm Panonun Durumu + Çözüm Sonuçları

### En büyük tip. İki görevi var:
1. **Giriş:** Kullanıcının kurduğu devre (parçalar + kablolar + buton durumları)
2. **Çıkış:** Solver'ın hesapladığı sonuçlar (motor durumu, arızalar)

### Kod (kısaltılmış):
```csharp
public class PanelState
{
    // ─── GİRİŞ (kullanıcının kurduğu devre) ──────────────────────────────
    public List<Placement> Parts { get; set; }    // Tüm parçalar
    public List<Wire> Wires { get; set; }         // Tüm kablolar
    public bool PowerOn { get; set; }             // Ana otomat açık mı?
    public bool StartPressed { get; set; }         // START basılı mı?
    public bool StopPressed { get; set; }          // STOP basılı mı?
    public bool SimulatedThermalTrip { get; set; } // Termik simülasyonu

    // ─── ÇIKIŞ (solver'ın hesapladığı) ───────────────────────────────────
    public List<NetResult> NetResults { get; set; }  // Ağ analizi sonuçları
    public int SolverIterations { get; set; }         // Kaç iterasyonda kararlı oldu?
    public bool IsOscillating { get; set; }           // Salınımda mı? (flaşör)
    public bool MainBreakerTripped { get; set; }      // Kısa devrede otomat açtı mı?
    public int MotorPhaseCount { get; set; }          // Motora gelen faz sayısı (0,1,2,3)
    public bool MotorGrounded { get; set; }           // Motor PE bağlı mı?
}
```

### `NetResult` — Ağ (Net) Analizi Sonucu:
```csharp
public class NetResult
{
    public HashSet<Potential> Potentials { get; set; }  // Bu ağda hangi gerilimler var?

    // Otomatik hesaplanan özellikler:
    public bool HasShortCircuit  // L1+L2 aynı nette → KISA DEVRE!
    public bool HasGroundFault   // L1+PE aynı nette → ELEKTRİK KAÇAĞI!
}
```

### `Potential` enum — Gerilim Seviyeleri:
```csharp
public enum Potential
{
    Unknown,   // Belirsiz (bağlanmamış)
    L1,        // Faz 1 (R fazı)
    L2,        // Faz 2 (S fazı)
    L3,        // Faz 3 (T fazı)
    Neutral,   // Nötr (N)
    PE         // Koruma iletkeni (toprak)
}
```

---

## Tipler Arası İlişki (Özet)

```
PartDefinition    →   Placement
(Katalog şablonu)     (Panodaki örnek)
                           ↓
                       PartState
                    (Anlık durum — değişiyor)

Placement + Wire  →   PanelState
(Parçalar+kablolar)   (Tüm panonun anlık hali)
                           ↓
                       CircuitSolver (Faz 2)
                           ↓
                       NetResult × N
                    (Her ağ için analiz sonucu)
```
