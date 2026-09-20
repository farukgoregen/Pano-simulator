using PanoSimulator.Engine.Solver;
using PanoSimulator.Engine.Types;

namespace PanoSimulator.Engine.Tests;

/// <summary>
/// Faz 3 — Arıza Tespiti Testleri
///
/// Kısa Devre, PE Kaçağı ve Faz Eksikliği durumlarını test eder.
/// </summary>
public class Phase3FaultTests
{
    private static (PanelState state, CircuitSolver solver) BuildFaultCircuit()
    {
        var solver = new CircuitSolver();

        // Arızaları simüle etmek için sadece basit parçalar kullanalım
        var motor = new Placement { Id = "m1", Type = "motor", State = new PartState() };
        var q1 = new Placement { Id = "q1", Type = "breaker", State = new PartState { BreakerClosed = true } };

        var state = new PanelState
        {
            Parts = [q1, motor],
            Wires = [],
            PowerOn = true
        };

        return (state, solver);
    }

    [Fact(DisplayName = "F01 - L1 ve L2 birleşirse Kısa Devre olur, otomat açar")]
    public void Fault_ShortCircuit_TripsBreaker()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("q1", "1"), WireColor.Black),
            new Wire(new EndPoint("__supply", "L2"), new EndPoint("q1", "1"), WireColor.Black) // L2 de aynı terminale!
        ];

        solver.Solve(state);

        Assert.True(state.MainBreakerTripped, "L1 ve L2 aynı nette buluştuğu için otomat açmalı");
        Assert.False(state.Parts.First(p => p.Id == "q1").State.BreakerClosed, "Q1 otomatı kapalı (açık) duruma gelmeli");
    }

    [Fact(DisplayName = "F02 - L1 ve Nötr birleşirse Kısa Devre olur")]
    public void Fault_ShortCircuit_PhaseNeutral()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("q1", "1"), WireColor.Black),
            new Wire(new EndPoint("__supply", "N"), new EndPoint("q1", "1"), WireColor.LightBlue) // Nötr de aynı yere!
        ];

        solver.Solve(state);
        Assert.True(state.MainBreakerTripped);
    }

    [Fact(DisplayName = "F03 - L1 fazı PE'ye değerse Toprak Kaçağı olur")]
    public void Fault_GroundFault_PhaseToPE()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("__supply", "PE"), WireColor.YellowGreen)
        ];

        solver.Solve(state);
        
        Assert.True(FaultDetector.CheckForGroundFault(state.NetResults));
    }

    [Fact(DisplayName = "F04 - Motora 3 faz gidiyorsa normal çalışma")]
    public void Fault_MotorPhases_3Phases()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("m1", "U"), WireColor.Black),
            new Wire(new EndPoint("__supply", "L2"), new EndPoint("m1", "V"), WireColor.Black),
            new Wire(new EndPoint("__supply", "L3"), new EndPoint("m1", "W"), WireColor.Black),
            new Wire(new EndPoint("__supply", "PE"), new EndPoint("m1", "PE"), WireColor.YellowGreen)
        ];

        solver.Solve(state);

        Assert.Equal(3, state.MotorPhaseCount);
        Assert.True(state.MotorGrounded);
    }

    [Fact(DisplayName = "F05 - Motora 2 faz gidiyorsa Faz Eksikliği (1 faz eksik)")]
    public void Fault_MotorPhases_MissingPhase()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("m1", "U"), WireColor.Black),
            // L2 eksik (bağlanmadı)
            new Wire(new EndPoint("__supply", "L3"), new EndPoint("m1", "W"), WireColor.Black),
            new Wire(new EndPoint("__supply", "PE"), new EndPoint("m1", "PE"), WireColor.YellowGreen)
        ];

        solver.Solve(state);

        Assert.Equal(2, state.MotorPhaseCount);
    }

    [Fact(DisplayName = "F06 - Motora L1 fazı her iki terminale giderse 1 faz sayılır (köprü)")]
    public void Fault_MotorPhases_BridgedPhase()
    {
        var (state, solver) = BuildFaultCircuit();
        state.Wires =
        [
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("m1", "U"), WireColor.Black),
            new Wire(new EndPoint("__supply", "L1"), new EndPoint("m1", "V"), WireColor.Black), // L1 iki yere giriyor
            new Wire(new EndPoint("__supply", "L3"), new EndPoint("m1", "W"), WireColor.Black)
        ];

        solver.Solve(state);

        Assert.Equal(2, state.MotorPhaseCount); // Sadece L1 ve L3 var (toplam 2 farklı faz)
    }
}
