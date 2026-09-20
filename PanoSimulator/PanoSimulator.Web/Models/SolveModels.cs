using PanoSimulator.Engine.Types;

namespace PanoSimulator.Web.Models;

public class SolveRequest
{
    public bool PowerOn { get; set; }
    public List<PlacementDto> Parts { get; set; } = new();
    public List<WireDto> Wires { get; set; } = new();
}

public class PlacementDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public PartStateDto State { get; set; } = new();
}

public class PartStateDto
{
    public bool IsPressed { get; set; }
    public bool CoilEnergized { get; set; }
    public double TimerElapsedMs { get; set; }
    public double TimerSetpointMs { get; set; }
}

public class WireDto
{
    public string Id { get; set; } = "";
    public string FromPart { get; set; } = "";
    public string FromTerminal { get; set; } = "";
    public string ToPart { get; set; } = "";
    public string ToTerminal { get; set; } = "";
    public string Color { get; set; } = "";
}

public class SolveResult
{
    public bool MainBreakerTripped { get; set; }
    public bool MotorGrounded { get; set; }
    public int MotorPhaseCount { get; set; }
    public string MotorConnection { get; set; } = ""; // "Star", "Delta", "Invalid", veya ""
    public List<string> Warnings { get; set; } = new();
    public List<string> Faults { get; set; } = new();
    public List<string> Info { get; set; } = new();
    public Dictionary<string, PartStateDto> PartStates { get; set; } = new();
}
