using PanoSimulator.Engine.Types;

namespace PanoSimulator.Engine.Components;

/// <summary>
/// Tüm desteklenen malzemelerin tanım kataloğu.
/// Her malzeme için terminal listesi ve InternalLinks mantığı burada tanımlı.
///
/// Terminal numaraları için kaynak:
///   Kontaktör: Kitap §2.2 + Siemens SIRIUS 3RT katalog + IEC 60947-4-1
///   Termik röle: Kitap §2.5 + IEC 60947-4-1
///   Buton: Kitap §2.9 + IEC 60947-5-1
/// </summary>
public static class ComponentCatalog
{
    private static readonly Dictionary<string, PartDefinition> _catalog = new();

    static ComponentCatalog()
    {
        RegisterAll();
    }

    /// <summary>Tip adına göre malzeme tanımı döner. Bilinmiyorsa null.</summary>
    public static PartDefinition? Get(string type)
        => _catalog.TryGetValue(type, out var def) ? def : null;

    /// <summary>Tüm kayıtlı malzemeleri döner (malzeme çekmecesi için)</summary>
    public static IReadOnlyCollection<PartDefinition> GetAll()
        => _catalog.Values;

    private static void RegisterAll()
    {
        RegisterPart(MakeBreaker());
        RegisterPart(MakeContactor());
        RegisterPart(MakeThermalRelay());
        RegisterPart(MakeButtonNO());
        RegisterPart(MakeButtonNC());
        RegisterPart(MakeMotor());
        RegisterPart(MakeLamp());
        RegisterPart(MakeTimerRelay());
    }

    private static void RegisterPart(PartDefinition def)
        => _catalog[def.Type] = def;

    // ─────────────────────────────────────────────────────────────────────
    // OTOMAT SİGORTA (Q1)  — Kitap §2.1
    // ─────────────────────────────────────────────────────────────────────
    // 3 kutuplu, her faz için bağımsız kontak çifti.
    // BreakerClosed=true  → 1-2, 3-4, 5-6 kapalı (akım geçiyor)
    // BreakerClosed=false → hepsi açık (kısa devre veya manüel açma)
    private static PartDefinition MakeBreaker() => new(
        Type: "breaker",
        Label: "Otomat Sigorta",
        Description: "3P otomat sigorta — aşırı akım ve kısa devreye karşı koruma",
        Terminals:
        [
            new("1",  0, 0),  // L1 giriş
            new("2",  0, 80), // L1 çıkış
            new("3", 20, 0),  // L2 giriş
            new("4", 20, 80), // L2 çıkış
            new("5", 40, 0),  // L3 giriş
            new("6", 40, 80), // L3 çıkış
        ],
        InternalLinks: state => state.BreakerClosed
            // Otomat kapalıysa üç faz geçiyor
            ? new[] { ("1","2"), ("3","4"), ("5","6") }
            // Açıksa hiçbir faz geçemiyor (kısa devre veya manüel açma)
            : Array.Empty<(string,string)>(),
        WidthUnits: 3,
        Category: PartCategory.Protection
    );

    // ─────────────────────────────────────────────────────────────────────
    // KONTAKTÖR (K1)  — Kitap §2.2, §3.1
    // ─────────────────────────────────────────────────────────────────────
    // Güç kontakları: 1-2, 3-4, 5-6 (3 faz motoru besler)
    // Yardımcı NO:    13-14 (mühürleme için kumanda devresine bağlanır)
    // Bobin:          A1 (besleme), A2 (dönüş/nötr)
    //
    // CoilEnergized=true  → NO kontaklar (1-2, 3-4, 5-6, 13-14) KAPANIR
    // CoilEnergized=false → NO kontaklar AÇILIR (yay geri çeker)
    //
    // NOT: NC yardımcı kontak (21-22) burada tanımlı değil.
    // İleri-geri devresinde (Faz 6) gerekecek, o zaman eklenecek.
    // CoilEnergized=true  → NO kontaklar (1-2, 3-4, 5-6, 13-14) KAPANIR, NC (21-22) AÇILIR
    // CoilEnergized=false → NO kontaklar AÇILIR, NC (21-22) KAPANIR
    private static PartDefinition MakeContactor() => new(
        Type: "contactor",
        Label: "Kontaktör",
        Description: "3 fazlı kontaktör — bobin çekince motor devresi kapanır",
        Terminals:
        [
            new("1", 0, 0),    // Güç girişi L1
            new("2", 0, 80),   // Güç çıkışı T1
            new("3", 20, 0),   // Güç girişi L2
            new("4", 20, 80),  // Güç çıkışı T2
            new("5", 40, 0),   // Güç girişi L3
            new("6", 40, 80),  // Güç çıkışı T3
            new("13", 60, 0),  // Yardımcı NO (Mühürleme) giriş
            new("14", 60, 80), // Yardımcı NO çıkış
            new("21", 80, 0),  // Yardımcı NC (Kilitleme) giriş
            new("22", 80, 80), // Yardımcı NC çıkış
            new("A1", 100, 0),  // Bobin besleme (L1 veya L'den)
            new("A2", 100, 80), // Bobin dönüş (N)
        ],
        InternalLinks: state => state.CoilEnergized
            // Bobin çekti → güç kontakları + yardımcı NO kapandı, NC açıldı
            ? new[] { ("1","2"), ("3","4"), ("5","6"), ("13","14") }
            // Bobin düşük → hiç NO kontak kapalı değil, NC kapalı
            : new[] { ("21","22") },
        WidthUnits: 6,
        Category: PartCategory.Switching
    );

    // ─────────────────────────────────────────────────────────────────────
    // TERMİK RÖLE (F2)  — Kitap §2.5
    // ─────────────────────────────────────────────────────────────────────
    // Motor akımını bimetal elemana göre ölçer.
    // ThermalTripped=false → 95-96 NC KAPALI (kumanda devresinden akım geçer)
    // ThermalTripped=true  → 95-96 NC AÇILIR → kumanda kesilir → kontaktör düşer
    //                        97-98 NO KAPANIR → arıza lambası yakılabilir
    //
    // Neden 95-96 kumanda devresine alınır?
    // Termik akmakla direkt motoru kesmez — K1 bobinini enerjisiz bırakır,
    // K1 da gücü keser. Bu sayede kontaktörün ömrü korunur.
    private static PartDefinition MakeThermalRelay() => new(
        Type: "thermalRelay",
        Label: "Termik Röle",
        Description: "Motor aşırı akım koruması — 95-96 (NC) kumanda devresine alınır",
        Terminals:
        [
            new("95", 0, 0),  // NC kontak giriş
            new("96", 0, 80), // NC kontak çıkış  → kumanda devresine seri
            new("97", 20, 0), // NO kontak giriş
            new("98", 20, 80),// NO kontak çıkış  → arıza lambası için
        ],
        InternalLinks: state => state.ThermalTripped
            // Termik ATTI → NC (95-96) açıldı, NO (97-98) kapandı
            ? new[] { ("97","98") }
            // Termik NORMAL → NC (95-96) kapalı, NO (97-98) açık
            : new[] { ("95","96") },
        WidthUnits: 3,
        Category: PartCategory.Protection
    );

    // ─────────────────────────────────────────────────────────────────────
    // NO BUTON (START)  — Kitap §2.9
    // ─────────────────────────────────────────────────────────────────────
    // Normalde Açık: Basılmadıkça akım geçmez.
    // Basılınca kapanır → K1 bobinine anlık gerilim gider.
    // Bırakılınca tekrar açılır — MÜHÜRLEMESİZ devreler bu yüzden çalışmaz!
    // (Mühürleme: K1'in 13-14 kontağı ile çözülür)
    private static PartDefinition MakeButtonNO() => new(
        Type: "button-no",
        Label: "NO Buton (START)",
        Description: "Normalde Açık — basıldığında devreyi kapar",
        Terminals:
        [
            new("3", 0, 0),   // NO giriş (IEC 60947-5-1: tek elemanlı butonda 3-4)
            new("4", 0, 60),  // NO çıkış
        ],
        InternalLinks: state => state.IsPressed
            // Basılı → kontak KAPALI
            ? new[] { ("3","4") }
            // Serbest → kontak AÇIK (NO = normalde açık)
            : Array.Empty<(string,string)>(),
        WidthUnits: 2,
        Category: PartCategory.PushButton
    );

    // ─────────────────────────────────────────────────────────────────────
    // NC BUTON (STOP)  — Kitap §2.9
    // ─────────────────────────────────────────────────────────────────────
    // Normalde Kapalı: Basılmadıkça akım geçer.
    // Basılınca açılır → devre kesilir → K1 bobini enerjisiz → motor durur.
    // Neden NC? Kablo kopsa veya buton arızalansa bile STOP güvenli çalışır.
    // ("Fail-safe" tasarım — güvenlik prensibi)
    private static PartDefinition MakeButtonNC() => new(
        Type: "button-nc",
        Label: "NC Buton (STOP)",
        Description: "Normalde Kapalı — basıldığında devreyi açar (fail-safe)",
        Terminals:
        [
            new("1", 0, 0),   // NC giriş
            new("2", 0, 60),  // NC çıkış
        ],
        InternalLinks: state => state.IsPressed
            // Basılı → kontak AÇIK (NC = normalde kapalı, basılınca açılır)
            ? Array.Empty<(string,string)>()
            // Serbest → kontak KAPALI (akım geçiyor)
            : new[] { ("1","2") },
        WidthUnits: 2,
        Category: PartCategory.PushButton
    );

    // ─────────────────────────────────────────────────────────────────────
    // MOTOR (M1)  — Kitap §1.1.2 ve §3.17 (Yıldız-Üçgen)
    // ─────────────────────────────────────────────────────────────────────
    // 3 fazlı asenkron motor. Güç terminalleri:
    // Klemens kutusu dizilimi (IEC):
    // Üst: U1, V1, W1, PE
    // Alt: W2, U2, V2
    // Bu sayede üçgen bağlantı (U1-W2, V1-U2, W1-V2) düz köprülerle yapılır.
    private static PartDefinition MakeMotor() => new(
        Type: "motor",
        Label: "Asenkron Motor",
        Description: "3 fazlı asenkron motor (6 Uçlu) — Yıldız/Üçgen uyumlu",
        Terminals:
        [
            new("U1",  0, 0),   // Faz 1 girişi
            new("V1",  20, 0),  // Faz 2 girişi
            new("W1",  40, 0),  // Faz 3 girişi
            new("PE",  60, 0),  // Koruma iletkeni (Toprak)
            
            new("W2",  0, 80),  // Sargı 3 çıkışı
            new("U2",  20, 80), // Sargı 1 çıkışı
            new("V2",  40, 80), // Sargı 2 çıkışı
        ],
        // Motor sargıları içeriden U1-U2, V1-V2, W1-W2 şeklinde bağlıdır.
        // Ancak bu bağlantı doğrudan kısa devre gibi değil, empedanslıdır.
        // CircuitSolver, yıldız/üçgen yapısını sargılara gelen voltajlarla algılar.
        InternalLinks: _ => Array.Empty<(string,string)>(),
        WidthUnits: 5,
        Category: PartCategory.Field
    );

    // ─────────────────────────────────────────────────────────────────────
    // SİNYAL LAMBASI (H)  — Kitap §2.12
    // ─────────────────────────────────────────────────────────────────────
    // SİNYAL LAMBASI (H1) — Kitap §2.8
    // ─────────────────────────────────────────────────────────────────────
    private static PartDefinition MakeLamp() => new(
        Type: "lamp",
        Label: "Sinyal Lambası",
        Description: "220V AC Sinyal Lambası",
        Terminals:
        [
            new("X1", 0, 0),
            new("X2", 0, 80)
        ],
        InternalLinks: _ => Array.Empty<(string, string)>(),
        WidthUnits: 2,
        Category: PartCategory.Indicator
    );

    // ─────────────────────────────────────────────────────────────────────
    // ZAMAN RÖLESİ (KT - TON) — Kitap §4.2 (Faz 7)
    // ─────────────────────────────────────────────────────────────────────
    // ZAMAN RÖLESİ (TON - Faz 7)
    // ─────────────────────────────────────────────────────────────────────
    // TimerElapsedMs >= TimerSetpointMs olunca kontak değiştirir.
    private static PartDefinition MakeTimerRelay() => new(
        Type: "timer",
        Label: "Zaman Rölesi",
        Description: "TON (Çekmede Gecikmeli) - Süre dolunca kontak değiştirir",
        Terminals:
        [
            new("A1", 0, 0),   // Bobin Faz
            new("A2", 0, 80),  // Bobin Nötr
            new("15", 40, 0),  // Ortak uç
            new("16", 20, 80), // NC uç (açılır)
            new("18", 60, 80)  // NO uç (kapanır)
        ],
        InternalLinks: state => 
        {
            if (state.CoilEnergized && state.TimerElapsedMs >= state.TimerSetpointMs)
                return new[] { ("15", "18") }; // Süre doldu, NO kapandı
            
            return new[] { ("15", "16") };
        },
        WidthUnits: 4,
        Category: PartCategory.Relay
    );

    // ─────────────────────────────────────────────────────────────────────
    // LİMİT SWİTCH (Sınır Anahtarı - Faz 8)
    // ─────────────────────────────────────────────────────────────────────
    // Mekanik bir sensördür. Çarpıldığında kontakları değiştirir.
    // 11-12 NC, 13-14 NO (IEC normu)
    private static PartDefinition MakeLimitSwitch() => new(
        Type: "limitSwitch",
        Label: "Limit Sensörü",
        Description: "Fiziksel çarpma ile konum değiştiren anahtar",
        Terminals:
        [
            new("11", 0, 0),  // NC giriş
            new("12", 0, 60), // NC çıkış
            new("13", 20, 0), // NO giriş
            new("14", 20, 60) // NO çıkış
        ],
        InternalLinks: state => state.IsPressed
            ? new[] { ("13", "14") } // Basıldıysa NO kapanır
            : new[] { ("11", "12") }, // Basılı değilse NC kapalıdır
        WidthUnits: 2,
        Category: PartCategory.Field
    );
}
