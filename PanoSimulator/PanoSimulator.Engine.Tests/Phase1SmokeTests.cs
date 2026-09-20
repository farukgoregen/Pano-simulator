using PanoSimulator.Engine.Types;
using PanoSimulator.Engine.Components;
using PanoSimulator.Engine.Solver;

namespace PanoSimulator.Engine.Tests;

/// <summary>
/// Faz 1 doğrulama testleri.
/// Henüz çözücü implement edilmedi — bu testler sadece:
///   1. Engine projesinin build ettiğini
///   2. Tip sisteminin doğru çalıştığını
///   3. ComponentCatalog'un erişilebilir olduğunu
/// kanıtlar.
/// Faz 2'de gerçek devre testleri buraya eklenecek.
/// </summary>
public class Phase1SmokeTests
{
    [Fact(DisplayName = "Terminal kaydı doğru çalışıyor")]
    public void Terminal_RecordEquality_Works()
    {
        var t1 = new Terminal("A1", 0, 0);
        var t2 = new Terminal("A1", 0, 0);
        Assert.Equal(t1, t2); // record value equality
    }

    [Fact(DisplayName = "Wire rengi IEC enum değerlerine sahip")]
    public void WireColor_HasAllIecColors()
    {
        // IEC 60204-1 Tablo 4 — altı standart renk olmalı
        var colors = Enum.GetValues<WireColor>();
        Assert.Equal(6, colors.Length);
        Assert.Contains(WireColor.Black, colors);
        Assert.Contains(WireColor.Red, colors);
        Assert.Contains(WireColor.YellowGreen, colors); // PE — başka amaçla kullanılamaz
    }

    [Fact(DisplayName = "PanelState başlangıçta güvenli durumda")]
    public void PanelState_InitialState_IsSafe()
    {
        var state = new PanelState();
        Assert.False(state.PowerOn);         // Güç kapalı
        Assert.False(state.StartPressed);    // Start basılı değil
        Assert.False(state.MainBreakerTripped); // Otomat açmamış
        Assert.Equal(0, state.MotorPhaseCount); // Motor durmuş
    }

    [Fact(DisplayName = "PartState kontaktör başlangıçta düşük")]
    public void PartState_Contactor_StartsDeenergized()
    {
        var state = new PartState();
        Assert.False(state.CoilEnergized);      // Bobin çekmemiş
        Assert.False(state.ThermalTripped);     // Termik atmamış
        Assert.True(state.BreakerClosed);       // Otomat kapalı (geçiriyor)
    }

    [Fact(DisplayName = "NetResult kısa devre tespiti — iki faz aynı nette")]
    public void NetResult_TwoPhasesInSameNet_IsShortCircuit()
    {
        var net = new NetResult
        {
            Potentials = [Potential.L1, Potential.L2]
        };
        Assert.True(net.HasShortCircuit);
    }

    [Fact(DisplayName = "NetResult PE kaçağı tespiti")]
    public void NetResult_PhaseAndPeInSameNet_IsGroundFault()
    {
        var net = new NetResult
        {
            Potentials = [Potential.L1, Potential.PE]
        };
        Assert.True(net.HasGroundFault);
    }

    [Fact(DisplayName = "NetResult normal net — kısa devre yok")]
    public void NetResult_SinglePhase_NoFault()
    {
        var net = new NetResult
        {
            Potentials = [Potential.L1]
        };
        Assert.False(net.HasShortCircuit);
        Assert.False(net.HasGroundFault);
    }

    [Fact(DisplayName = "ComponentCatalog Faz 1'de boş ama erişilebilir")]
    public void ComponentCatalog_ReturnsEmpty_InPhase1()
    {
        var all = ComponentCatalog.GetAll();
        Assert.NotNull(all); // null değil, sadece boş
        // Faz 2'de: Assert.NotEmpty(all);
    }

    [Fact(DisplayName = "CircuitSolver.Solve hata fırlatmıyor")]
    public void CircuitSolver_Solve_DoesNotThrow()
    {
        var solver = new CircuitSolver();
        var state = new PanelState
        {
            PowerOn = true,
            Parts = [new Placement { Id = "k1", Type = "contactor", State = new PartState() }]
        };

        // Faz 2'de çözücü tam çalışıyor — exception fırlatmamalı
        var ex = Record.Exception(() => solver.Solve(state));
        Assert.Null(ex);
        Assert.True(state.SolverIterations > 0);
    }

    [Fact(DisplayName = "EndPoint ve Wire kaydı oluşturuluyor")]
    public void Wire_CanBeCreated()
    {
        var wire = new Wire(
            A: new EndPoint("k1", "13"),
            B: new EndPoint("bt_start", "3"),
            Color: WireColor.Red  // Kumanda devresi — IEC 60204-1
        );
        Assert.Equal("k1", wire.A.PlacementId);
        Assert.Equal("13", wire.A.TerminalId);  // K1 yardımcı NO kontağı
        Assert.Equal(WireColor.Red, wire.Color); // Kumanda devresi kırmızı
    }
}
