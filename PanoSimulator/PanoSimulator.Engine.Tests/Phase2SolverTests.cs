using PanoSimulator.Engine.Components;
using PanoSimulator.Engine.Solver;
using PanoSimulator.Engine.Types;

namespace PanoSimulator.Engine.Tests;

/// <summary>
/// Faz 2 — Simülasyon Motoru Testleri
///
/// Test edilen devre: Start-stop mühürlemeli doğrudan yol verme (Kitap §3.6)
///
/// DEVRE ŞEMASI:
/// Güç:    L1 → Q1(1-2) → K1(1-2) → Motor(U)   [L1 fazı]
///         L2 → Q1(3-4) → K1(3-4) → Motor(V)   [L2 fazı]
///         L3 → Q1(5-6) → K1(5-6) → Motor(W)   [L3 fazı]
///
/// Kumanda: L1 → f1(sigorta) → STOP_NC(1-2) → [START_NO(3-4) ∥ K1_muh(13-14)]
///               → F2_termik(95-96) → K1_bobin(A1) → N(A2)
///
/// Mühürleme: K1(13-14) START butonuna paralel bağlı.
/// K1 çekince 13-14 kapanır → START bırakılsa bile bobin enerjili kalır.
/// </summary>
public class Phase2SolverTests
{
    // ─── Yardımcı: Standart Start-Stop Devresini Kur ─────────────────────────
    // Bu metod her testte aynı devreyi kurar — sadece state farklı olacak.

    private static (PanelState state, CircuitSolver solver) BuildStartStopCircuit(
        bool startPressed = false,
        bool stopPressed = false,
        bool thermalTripped = false,
        bool powerOn = true)
    {
        var solver = new CircuitSolver();

        // ── Parçalar ──────────────────────────────────────────────────────────
        var q1 = new Placement { Id = "q1",       Type = "breaker",     State = new PartState { BreakerClosed = true } };
        var k1 = new Placement { Id = "k1",       Type = "contactor",   State = new PartState { CoilEnergized = false } };
        var f2 = new Placement { Id = "f2",       Type = "thermalRelay",State = new PartState { ThermalTripped = thermalTripped } };
        var btStart = new Placement { Id = "bt_start", Type = "button-no",  State = new PartState { IsPressed = startPressed } };
        var btStop  = new Placement { Id = "bt_stop",  Type = "button-nc",  State = new PartState { IsPressed = stopPressed } };
        var motor   = new Placement { Id = "m1",       Type = "motor",      State = new PartState() };

        // ── Kablolar ─────────────────────────────────────────────────────────
        // Güç devresi: L1/L2/L3 → Q1 → K1 → Motor
        // Besleme kaynakları "__supply_L1" vb. özel adlarla tanımlı
        var wires = new List<Wire>
        {
            // L1 → Q1 terminal 1
            new(new EndPoint("__supply", "L1"), new EndPoint("q1", "1"), WireColor.Black),
            // L2 → Q1 terminal 3
            new(new EndPoint("__supply", "L2"), new EndPoint("q1", "3"), WireColor.Black),
            // L3 → Q1 terminal 5
            new(new EndPoint("__supply", "L3"), new EndPoint("q1", "5"), WireColor.Black),

            // Q1 çıkış → K1 güç girişi
            new(new EndPoint("q1", "2"), new EndPoint("k1", "1"), WireColor.Black),
            new(new EndPoint("q1", "4"), new EndPoint("k1", "3"), WireColor.Black),
            new(new EndPoint("q1", "6"), new EndPoint("k1", "5"), WireColor.Black),

            // K1 güç çıkışı → Motor
            new(new EndPoint("k1", "2"), new EndPoint("m1", "U"), WireColor.Black),
            new(new EndPoint("k1", "4"), new EndPoint("m1", "V"), WireColor.Black),
            new(new EndPoint("k1", "6"), new EndPoint("m1", "W"), WireColor.Black),

            // Motor PE → Toprak
            new(new EndPoint("m1", "PE"), new EndPoint("__supply", "PE"), WireColor.YellowGreen),

            // ── Kumanda devresi (kırmızı kablo — IEC 60204-1) ──────────────
            // L1 → STOP butonu NC giriş
            new(new EndPoint("__supply", "L1"), new EndPoint("bt_stop", "1"), WireColor.Red),
            // STOP NC çıkış → START NO giriş (seri bağlı)
            new(new EndPoint("bt_stop", "2"), new EndPoint("bt_start", "3"), WireColor.Red),
            // START NO çıkış → K1 terminal 14 (mühürleme dalı çıkışına birleştir)
            new(new EndPoint("bt_start", "4"), new EndPoint("k1", "14"), WireColor.Red),
            // K1 terminal 14 → Termik 95 (mühürleme kontağından sonra termik)
            new(new EndPoint("k1", "14"), new EndPoint("f2", "95"), WireColor.Red),
            // Termik 96 → K1 bobin A1
            new(new EndPoint("f2", "96"), new EndPoint("k1", "A1"), WireColor.Red),
            // K1 bobin A2 → Nötr
            new(new EndPoint("k1", "A2"), new EndPoint("__supply", "N"), WireColor.LightBlue),

            // Mühürleme: K1(13-14) START butonu ile paralel
            // bt_stop(2) → k1(13) (mühürleme dalı)
            new(new EndPoint("bt_stop", "2"), new EndPoint("k1", "13"), WireColor.Red),
        };

        var panelState = new PanelState
        {
            Parts    = [q1, k1, f2, btStart, btStop, motor],
            Wires    = wires,
            PowerOn  = powerOn,
            StartPressed = startPressed,
            StopPressed  = stopPressed,
            SimulatedThermalTrip = thermalTripped,
        };

        return (panelState, solver);
    }

    // ─── Besleme kaynağı için özel EndPoint mapping ───────────────────────────
    // CircuitSolver "__supply_L1" gibi adlar kullanıyor.
    // Testlerde "__supply" placement ile "L1" terminal yazdık.
    // Bu iki format arasında köprü için solver'a "__supply:L1" → "__supply_L1" map'i lazım.
    // Bunun yerine daha net bir yaklaşım kullanalım:
    // Wire'da PlacementId="__supply_L1", TerminalId="" olarak yaz.

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 01 — Boşta Durum
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "01 Boşta: Enerji var, hiç buton yok → K1 düşük, motor durmuş")]
    public void T01_Idle_ContactorOff_MotorStopped()
    {
        var (state, solver) = BuildStartStopCircuit(powerOn: true);
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.False(k1.State.CoilEnergized, "K1 bobini boşta çekmemeli");
        Assert.Equal(0, state.MotorPhaseCount);
        Assert.False(state.MainBreakerTripped, "Kısa devre yok — otomat açmamalı");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 02 — START Basıldı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "02 START: Butona basılınca K1 çekiyor, motor 3 fazdan besleniyor")]
    public void T02_Start_ContactorEnergizes_MotorRunning()
    {
        var (state, solver) = BuildStartStopCircuit(startPressed: true);
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.True(k1.State.CoilEnergized, "START basıldığında K1 bobin çekmeli");
        Assert.Equal(3, state.MotorPhaseCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 03 — Mühürleme (En Kritik Test!)
    // START basıp bırakıyoruz — mühürleme sayesinde K1 enerjili kalmalı.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "03 Mühürleme: START bırakılınca K1 hâlâ enerjili (mühürleme çalışıyor)")]
    public void T03_Sealing_ContactorStaysOn_AfterStartReleased()
    {
        // Adım 1: START'a bas → K1 çeksin
        var (state, solver) = BuildStartStopCircuit(startPressed: true);
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.True(k1.State.CoilEnergized, "Önce K1 çekmeli");

        // Adım 2: START'ı bırak — mühürleme devreye girmeli
        state.StartPressed = false;
        // NOT: CoilEnergized state'i SIFIRLANMIYOR — solver önceki durumdan başlar.
        // Bu mühürlemenin çalışmasının sırrı: K1.13-14 hâlâ kapalı çünkü
        // CoilEnergized hâlâ true → InternalLinks → (13,14) bağlı.
        solver.Solve(state);

        Assert.True(k1.State.CoilEnergized,
            "START bırakılınca K1 DÜŞMEMELİ — mühürleme kontağı (13-14) devreyi sürdürüyor");
        Assert.Equal(3, state.MotorPhaseCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 04 — STOP Basıldı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "04 STOP: Butona basılınca K1 düşüyor, motor duruyor")]
    public void T04_Stop_ContactorDrops_MotorStops()
    {
        // Önce çalışır hale getir
        var (state, solver) = BuildStartStopCircuit(startPressed: true);
        solver.Solve(state);

        // START bırak (mühürleme)
        state.StartPressed = false;
        solver.Solve(state);

        // STOP bas
        state.StopPressed = true;
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.False(k1.State.CoilEnergized, "STOP basılınca K1 düşmeli");
        Assert.Equal(0, state.MotorPhaseCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 05 — Termik Attı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "05 Termik: Termik atınca K1 düşüyor (95-96 NC açıldı)")]
    public void T05_ThermalTrip_ContactorDrops()
    {
        // Çalışır hale getir
        var (state, solver) = BuildStartStopCircuit(startPressed: true);
        solver.Solve(state);
        state.StartPressed = false;
        solver.Solve(state); // Mühürleme devrede

        // Termik attır
        state.SimulatedThermalTrip = true;
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.False(k1.State.CoilEnergized,
            "Termik atınca 95-96 NC açılır → kumanda kesilir → K1 düşer");
        Assert.Equal(0, state.MotorPhaseCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 06 — Güç Kapalıyken Hiçbir Şey Çalışmıyor
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "06 Güç yok: PowerOn=false iken motor durmuş, K1 düşük")]
    public void T06_NoPower_EverythingOff()
    {
        var (state, solver) = BuildStartStopCircuit(startPressed: true, powerOn: false);
        solver.Solve(state);

        var k1 = state.Parts.First(p => p.Id == "k1");
        Assert.False(k1.State.CoilEnergized);
        Assert.Equal(0, state.MotorPhaseCount);
        Assert.Equal(0, state.SolverIterations); // Hızlı çıkış
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 07 — ComponentCatalog Eksiksiz
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "07 Katalog: Tüm 7 malzeme kayıtlı")]
    public void T07_Catalog_HasAllComponents()
    {
        var all = ComponentCatalog.GetAll();
        Assert.Equal(7, all.Count); // breaker, contactor, thermalRelay, button-no, button-nc, motor, lamp

        Assert.NotNull(ComponentCatalog.Get("contactor"));
        Assert.NotNull(ComponentCatalog.Get("thermalRelay"));
        Assert.NotNull(ComponentCatalog.Get("button-no"));
        Assert.NotNull(ComponentCatalog.Get("button-nc"));
        Assert.NotNull(ComponentCatalog.Get("motor"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 08 — Kontaktör Terminal Sayıları
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "08 Kontaktör: 10 terminal (1-6 güç + 13-14 yardımcı + A1-A2 bobin)")]
    public void T08_Contactor_HasCorrectTerminals()
    {
        var def = ComponentCatalog.Get("contactor");
        Assert.NotNull(def);
        Assert.Equal(10, def!.Terminals.Count);

        var ids = def.Terminals.Select(t => t.Id).ToHashSet();
        Assert.Contains("A1", ids);   // Bobin besleme
        Assert.Contains("A2", ids);   // Bobin dönüş
        Assert.Contains("13", ids);   // Yardımcı NO giriş
        Assert.Contains("14", ids);   // Yardımcı NO çıkış
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 09 — Termik Röle InternalLinks Doğruluğu
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "09 Termik: Normal durumda 95-96 kapalı, atınca 97-98 kapalı")]
    public void T09_ThermalRelay_InternalLinksCorrect()
    {
        var def = ComponentCatalog.Get("thermalRelay");
        Assert.NotNull(def);

        var normalState = new PartState { ThermalTripped = false };
        var links = def!.InternalLinks(normalState).ToList();
        Assert.Contains(("95", "96"), links);   // NC kapalı → kumanda devresinden geçer
        Assert.DoesNotContain(("97", "98"), links); // NO açık

        var trippedState = new PartState { ThermalTripped = true };
        links = def.InternalLinks(trippedState).ToList();
        Assert.Contains(("97", "98"), links);   // NO kapandı → arıza lambası
        Assert.DoesNotContain(("95", "96"), links); // NC açıldı → kumanda kesildi
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 10 — Union-Find Temel Doğruluk
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "10 UnionFind: Birleştirme ve bağlantı kontrolü doğru")]
    public void T10_UnionFind_BasicCorrectness()
    {
        var uf = new UnionFind(5);  // 5 düğüm: 0,1,2,3,4

        uf.Union(0, 1); // 0-1 bağlı
        uf.Union(2, 3); // 2-3 bağlı

        Assert.True(uf.Connected(0, 1));    // Bağlı
        Assert.False(uf.Connected(0, 2));   // Bağlı değil
        Assert.False(uf.Connected(1, 3));   // Bağlı değil
        Assert.Equal(3, uf.ComponentCount()); // 3 küme: {0,1}, {2,3}, {4}

        uf.Union(1, 2); // Şimdi {0,1,2,3}
        Assert.True(uf.Connected(0, 3));    // Transitif bağlantı
        Assert.Equal(2, uf.ComponentCount()); // 2 küme: {0,1,2,3}, {4}
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 11 — NC Buton Başlangıçta Kapalı
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "11 NC Buton: Basılmadığında kapalı (fail-safe doğru)")]
    public void T11_NCButton_DefaultlyClosed()
    {
        var def = ComponentCatalog.Get("button-nc");
        Assert.NotNull(def);

        // Basılmamış → NC kapalı (akım geçiyor)
        var notPressed = new PartState { IsPressed = false };
        var links = def!.InternalLinks(notPressed).ToList();
        Assert.Contains(("1", "2"), links);

        // Basılmış → NC açık (akım kesildi)
        var pressed = new PartState { IsPressed = true };
        links = def.InternalLinks(pressed).ToList();
        Assert.Empty(links); // Hiç iç bağlantı yok
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 12 — Motor Topraklama Kontrolü
    // ─────────────────────────────────────────────────────────────────────────
    [Fact(DisplayName = "12 Motor: PE bağlı değilse MotorGrounded=false")]
    public void T12_Motor_GroundingCheck()
    {
        var solver = new CircuitSolver();
        var state = new PanelState
        {
            PowerOn = true,
            Parts = [new Placement { Id = "m1", Type = "motor", State = new PartState() }],
            Wires = [] // PE kablosu yok
        };

        solver.Solve(state);

        Assert.False(state.MotorGrounded, "PE bağlantısı olmayan motor topraklanmamış sayılmalı");
    }
}
