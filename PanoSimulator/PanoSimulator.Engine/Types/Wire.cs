namespace PanoSimulator.Engine.Types;

/// <summary>
/// Kablo renkleri — IEC 60204-1 Tablo 4 (Kitap §2, kablo renk standardı).
/// Renk seçimi sadece görsel değil, seviye kuralı olarak da denetlenir:
///   Güç devresine kırmızı kablo çekilmişse → uyarı
///   PE hattına sarı-yeşil dışında kablo → uyarı
/// </summary>
public enum WireColor
{
    Black,
    Brown,
    Gray,
    Red,
    Blue,
    White,
    Pink,
    Purple,
    Green
}

/// <summary>
/// Bir kablonun ucunu tanımlar: hangi parçanın hangi terminali.
/// </summary>
public record EndPoint(string PlacementId, string TerminalId);

/// <summary>
/// Panodaki iki terminal arasındaki kablo.
/// mm2 kesiti ileride kural kontrolünde kullanılacak
/// (motor akımı için minimum kesit — IEC 60204-1 §12).
/// </summary>
public record Wire(
    EndPoint A,
    EndPoint B,
    WireColor Color,
    double CrossSectionMm2 = 1.5
);
