namespace PanoSimulator.Engine.Types;

/// <summary>
/// Kullanıcının panoya yerleştirdiği bir parça örneği (instance).
/// Aynı tipten birden fazla parça olabilir (örn: iki kontaktör K1 ve K2).
/// </summary>
public class Placement
{
    /// <summary>
    /// Seviye içinde benzersiz kimlik: "k1", "f2", "bt_start" vb.
    /// Wire.EndPoint.PlacementId bu Id'ye referans verir.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Malzeme kataloğundaki tip: "contactor", "thermalRelay" vb.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Panoda X konumu (SVG koordinatı, piksel)</summary>
    public double X { get; set; }

    /// <summary>Panoda Y konumu (SVG koordinatı, piksel)</summary>
    public double Y { get; set; }

    /// <summary>
    /// Hangi DIN rayna hizalandı? (0=üst, 1=orta, 2=alt, -1=saha bölgesi).
    /// Snap algoritması bunu belirler.
    /// </summary>
    public int RailIndex { get; set; } = -1;

    /// <summary>Bu parçanın o anki çalışma durumu</summary>
    public PartState State { get; set; } = new();

    /// <summary>Kullanıcının parçaya verdiği etiket (isteğe bağlı, örn: "K1")</summary>
    public string UserLabel { get; set; } = string.Empty;
}
