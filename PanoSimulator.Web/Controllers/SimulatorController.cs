using Microsoft.AspNetCore.Mvc;
using PanoSimulator.Engine.Solver;
using PanoSimulator.Engine.Types;
using PanoSimulator.Web.Models;

namespace PanoSimulator.Web.Controllers;

public class SimulatorController : Controller
{
    private readonly IWebHostEnvironment _env;

    public SimulatorController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// /simulator/{id} — oyun ekranı.
    /// Faz 6'da: level JSON'ı okuyup View'a model olarak geçirilecek.
    /// Şimdilik: id'yi ViewBag ile geçiriyoruz, View placeholder gösteriyor.
    /// </summary>
    public IActionResult Play(int id)
    {
        // Geçersiz seviye — şimdilik 1-9 arası kabul ediyoruz
        if (id < 1 || id > 9)
            return RedirectToAction("Index", "Home");

        ViewBag.LevelId = id;
        return View();
    }

    [HttpPost]
    public IActionResult Solve([FromBody] SolveRequest request)
    {
        // 1. Gelen DTO'ları Engine tiplerine dönüştür
        var state = new PanelState { PowerOn = request.PowerOn };

        foreach (var pDto in request.Parts)
        {
            state.Parts.Add(new Placement 
            { 
                Id = pDto.Id, 
                Type = pDto.Type, 
                State = new PartState 
                {
                    IsPressed = pDto.State?.IsPressed ?? false,
                    CoilEnergized = pDto.State?.CoilEnergized ?? false,
                    TimerElapsedMs = pDto.State?.TimerElapsedMs ?? 0,
                    TimerSetpointMs = pDto.State?.TimerSetpointMs ?? 3000
                } 
            });
        }

        foreach (var wDto in request.Wires)
        {
            if (string.IsNullOrEmpty(wDto.FromTerminal) || string.IsNullOrEmpty(wDto.ToTerminal))
                continue;

            state.Wires.Add(new Wire(
                new EndPoint(wDto.FromPart, wDto.FromTerminal),
                new EndPoint(wDto.ToPart, wDto.ToTerminal),
                wDto.Color switch
                {
                    "brown" => WireColor.Brown,
                    "black" => WireColor.Black,
                    "gray" => WireColor.Gray,
                    "red" => WireColor.Red,
                    "blue" => WireColor.Blue,
                    "white" => WireColor.White,
                    "pink" => WireColor.Pink,
                    "purple" => WireColor.Purple,
                    "green" => WireColor.Green,
                    _ => WireColor.Black
                }
            ));
        }

        // 2. Çözücüyü çalıştır
        var solver = new CircuitSolver();
        solver.Solve(state);

        // 3. Sonuçları DTO'ya geri paketle
        var result = new SolveResult
        {
            MainBreakerTripped = state.MainBreakerTripped,
            MotorPhaseCount = state.MotorPhaseCount,
            MotorGrounded = state.MotorGrounded
        };
        
        foreach (var p in state.Parts)
        {
            result.PartStates[p.Id] = new PartStateDto 
            {
                IsPressed = p.State.IsPressed,
                CoilEnergized = p.State.CoilEnergized,
                TimerElapsedMs = p.State.TimerElapsedMs,
                TimerSetpointMs = p.State.TimerSetpointMs
            };
        }

        if (state.MainBreakerTripped)
            result.Faults.Add("Kısa devre algılandı! Ana otomat attı.");
        
        if (FaultDetector.CheckForGroundFault(state.NetResults))
            result.Faults.Add("Toprak kaçağı tespit edildi! (Faz, PE hattına temas ediyor)");

        var motor = state.Parts.FirstOrDefault(p => p.Type == "motor");
        if (motor != null)
        {
            if (!state.MotorGrounded)
                result.Warnings.Add("Motor gövdesi topraklanmamış (PE eksik)!");
            
            if (state.MotorPhaseCount == 3)
            {
                if (state.MotorConnection == "Star")
                    result.Info.Add("Motor YILDIZ (Star) bağlantıda sağlıklı çalışıyor.");
                else if (state.MotorConnection == "Delta")
                    result.Info.Add("Motor ÜÇGEN (Delta) bağlantıda sağlıklı çalışıyor.");
                else
                    result.Info.Add("Motor sağlıklı çalışıyor (3 Faz mevcut).");
            }
            else if (state.MotorPhaseCount > 0)
            {
                result.Faults.Add($"Motor faz eksikliği! Motora sadece {state.MotorPhaseCount} faz ulaşıyor.");
            }
        }

        return Json(result);
    }
}
