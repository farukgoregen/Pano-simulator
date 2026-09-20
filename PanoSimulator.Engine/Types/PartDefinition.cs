namespace PanoSimulator.Engine.Types;

/// <summary>
/// Malzeme kataloğundaki bir parça tanımı.
/// Faz 2'de ComponentCatalog.cs bu tip ile doldurulacak.
///
/// InternalLinks: Solver her tick'te bu fonksiyonu çağırır ve
/// o anki state'e göre hangİ terminal çiftlerinin iletken olduğunu öğrenir.
/// Örnek — Kontaktör K1, CoilEnergized=true iken:
///   InternalLinks(state) → [(1,2), (3,4), (5,6), (13,14)]  // NO kontaklar kapandı
/// </summary>
public record PartDefinition(
    /// <summary>Benzersiz tip adı: "contactor", "thermalRelay", "button-no", "motor" vb.</summary>
    string Type,

    /// <summary>Kullanıcıya gösterilecek Türkçe ad: "K1 Kontaktör", "F2 Termik Röle"</summary>
    string Label,

    /// <summary>Kısa açıklama (malzeme çekmecesi kartında gösterilir)</summary>
    string Description,

    /// <summary>
    /// Parçanın terminal listesi.
    /// Terminal Id'leri IEC/DIN standardına göre gerçek numaralar:
    ///   Kontaktör güç: "1","2","3","4","5","6"
    ///   Kontaktör bobin: "A1","A2"
    ///   Kontaktör yardımcı NO: "13","14"
    ///   Termik NC: "95","96"  Termik NO: "97","98"
    /// </summary>
    IReadOnlyList<Terminal> Terminals,

    /// <summary>
    /// O anki PartState'e göre iletken olan iç terminal çiftleri.
    /// Solver bu çiftleri Union-Find'a kenar olarak ekler.
    /// </summary>
    Func<PartState, IEnumerable<(string TermA, string TermB)>> InternalLinks,

    /// <summary>
    /// SVG çiziminde parçanın genişliği (DIN ray birimlerinde, 1 birim = 18mm)
    /// </summary>
    int WidthUnits = 4,

    /// <summary>Çekmecede hangi kategoride gösterilsin</summary>
    PartCategory Category = PartCategory.Protection
);

/// <summary>Malzeme çekmecesi kategorileri</summary>
public enum PartCategory
{
    /// <summary>Otomat, sigorta, termik röle</summary>
    Protection,
    /// <summary>Kontaktör, yardımcı kontaktör</summary>
    Switching,
    /// <summary>Zaman rölesi, sinyal rölesi</summary>
    Relay,
    /// <summary>Start, Stop, acil durum butonları</summary>
    PushButton,
    /// <summary>Sinyal lambaları</summary>
    Indicator,
    /// <summary>Motor (saha elemanı)</summary>
    Field,
    /// <summary>Klemens sırası</summary>
    Terminal
}
