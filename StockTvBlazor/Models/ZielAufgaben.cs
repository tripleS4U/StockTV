namespace StockTvBlazor.Models;

/// <summary>
/// Die vier Teile eines Ziel-Durchgangs (IER 502/507) mit Aufgabe je Versuch - dieselbe Tabelle wie
/// <c>ZielRegeln.Aufgabe</c> in StockSuite. Classic oder Compact ergibt sich aus der Zahl der Versuche je Teil
/// (<c>MaxKehrenProSpiel</c>: 6 = Classic, 4 = Compact); andere Zahlen bekommen nur den Teilnamen.
/// </summary>
public static class ZielAufgaben
{
	public static readonly string[] Namen = ["Maß Mitte", "Stockschießen", "Maß hinten", "Kombinieren"];

	public static string ErlaubteWerte(int teil) => teil == 1 ? "0 · 2 · 5 · 10" : "0 · 2 · 4 · 6 · 8 · 10";

	/// <summary>Aufgabe des Versuchs (1-basiert) in Teil 0–3, z. B. „Zielstock D“, „hinten links“, „Aufgeben · Zielstock G“.</summary>
	public static string Aufgabe(int teil, int versuch, int versucheProTeil)
	{
		var i = versuch - 1;
		if (i < 0 || i >= versucheProTeil) return "";
		var classic = versucheProTeil == 6;
		var compact = versucheProTeil == 4;
		return teil switch
		{
			0 => "Mitte",
			1 when classic || compact => $"Zielstock {"ABCDEF"[i]}",
			2 when classic || compact => i < versucheProTeil / 2 ? "hinten links" : "hinten rechts",
			3 when classic => new[] { "Ablenken · Zielstock A", "Ablenken · Zielstock B", "Aufgeben · Zielstock G", "Aufgeben · Zielstock H", "hinten links · Zielstock E", "hinten rechts · Zielstock F" }[i],
			3 when compact => new[] { "Aufgeben · Zielstock G", "Aufgeben · Zielstock H", "hinten links · Zielstock E", "hinten rechts · Zielstock F" }[i],
			_ => Namen[teil],
		};
	}
}
