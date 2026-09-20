namespace PanoSimulator.Engine.Types;

/// <summary>
/// Bir parçanın (kontaktör, termik röle, buton vb.) fiziksel bağlantı noktası.
/// Id, IEC/DIN standart terminal numaralarına karşılık gelir:
///   Kontaktör güç: 1/2, 3/4, 5/6
///   Kontaktör bobin: A1, A2
///   Kontaktör yardımcı NO: 13/14
///   Termik röle NC: 95/96  (kumanda devresine alınır — motor aşırı akım koruması)
///   Termik röle NO: 97/98
/// X, Y: parça kendi koordinat sisteminde terminal konumu (SVG çizimi için).
/// </summary>
public record Terminal(string Id, double X, double Y);
