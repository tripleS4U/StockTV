namespace StockTvBlazor.Settings;

public class GameSettings
{
	public enum Modus
	{
		Training = 0,
		BestOf = 1,
		Turnier = 2,
		Ziel = 100,
		Ziel2 = 101
	}

	public enum ZielAnsichtArt
	{
		/// <summary>Bisherige Seite /ziel - bleibt fuer die alte StockApp (NetMQ) die Vorgabe.</summary>
		Klassisch = 0,

		/// <summary>Seite /ziel-kacheln: vier Durchgangsteile als Kacheln, Aufgabe des naechsten Versuchs, Wertungsuhr.</summary>
		Kacheln = 1
	}

	public Modus CurrentModus { get; set; } = Modus.Training;

	/// <summary>Welche Seite die Modi Ziel/Ziel2 zeigen. Wird nur ueber REST (StockSuite) umgestellt.</summary>
	public ZielAnsichtArt ZielAnsicht { get; set; } = ZielAnsichtArt.Klassisch;

	public int MaxPunkteProKehre { get; set; } = 10;
	public int MaxKehrenProSpiel { get; set; } = 6;

	// Hier stand eine Liste "Kehren", die den Spielstand ueber einen Neustart retten sollte. Sie
	// trug seit jeher [JsonIgnore], landete also nie in der Datei - das Wiederherstellen beim
	// Start lief immer ins Leere, waehrend jede bestaetigte Kehre trotzdem einen vollstaendigen
	// Schreibvorgang ausloeste. Der Spielstand lebt allein in Match.Games.
}
