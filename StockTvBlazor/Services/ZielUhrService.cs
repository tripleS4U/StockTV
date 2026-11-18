namespace StockTvBlazor.Services;

/// <summary>Phase der Wertungsuhr im Zielwettbewerb.</summary>
public enum ZielUhrPhase
{
	/// <summary>Keine Uhr (Vorgabe, nach dem Start und wenn die Verbindung fehlt).</summary>
	Keine = 0,

	/// <summary>Einspielzeit vor dem Durchgang.</summary>
	Einspielen = 1,

	/// <summary>Wertungszeit eines Durchgangs (alle vier Teile: Mass Mitte, Stockschiessen, Mass hinten, Kombinieren).</summary>
	Durchgang = 2,
}

/// <summary>Was die Seite zeigt: Phase, Restzeit, Zustand.</summary>
public sealed record ZielUhrAnzeige(
	ZielUhrPhase Phase,
	int Durchgang,
	int DauerSekunden,
	int RestSekunden,
	bool Angehalten,
	bool Sichtbar)
{
	public bool Abgelaufen => Sichtbar && RestSekunden <= 0;

	/// <summary>„mm:ss“ (auch ueber 60 Minuten als Minuten).</summary>
	public string RestText => $"{Math.Max(0, RestSekunden) / 60:00}:{Math.Max(0, RestSekunden) % 60:00}";
}

/// <summary>
/// Wertungsuhr fuer den Zielwettbewerb. Sie kommt <b>nur von der Zentrale</b> (StockSuite, <c>PUT /api/v1/ziel/uhr</c>):
/// Die Zentrale schickt die Restzeit, das Geraet zaehlt mit seinem monotonen Zeitgeber weiter - die Tageszeit des Geraets
/// (Pi ohne Batterie-Uhr, oft falsch) spielt keine Rolle. Kommt laenger als <see cref="Verbindungsfrist"/> keine Meldung
/// (StockSuite schickt alle 30 s erneut), gilt die Verbindung als weg und die Uhr verschwindet: ohne Verbindung keine Uhr.
/// Thread-sicher (REST-Anfragen und Seiten-Timer).
/// </summary>
public sealed class ZielUhrService(TimeProvider zeit)
{
	/// <summary>So lange bleibt die Uhr ohne neue Meldung sichtbar.</summary>
	public static readonly TimeSpan Verbindungsfrist = TimeSpan.FromSeconds(90);

	private readonly Lock _sperre = new();
	private Stand? _stand;

	private sealed record Stand(ZielUhrPhase Phase, int Durchgang, int Dauer, int Rest, bool Angehalten, long Folge, long Empfangen);

	/// <summary>Neuer Stand von der Zentrale.</summary>
	public event Action? Geaendert;

	/// <summary>
	/// Uebernimmt einen Stand. Aeltere Meldungen (kleinere <paramref name="folge"/>) werden ignoriert, gleiche Folge
	/// frischt nur die Verbindung auf und gleicht die Restzeit ab.
	/// </summary>
	/// <returns>false, wenn die Meldung veraltet war.</returns>
	public bool Setzen(ZielUhrPhase phase, int durchgang, int dauerSekunden, int restSekunden, bool angehalten, long folge)
	{
		lock (_sperre)
		{
			if (_stand is { } alt && folge < alt.Folge)
				return false;
			_stand = phase == ZielUhrPhase.Keine
				? null
				: new Stand(phase, Math.Max(0, durchgang), Math.Max(0, dauerSekunden), Math.Clamp(restSekunden, 0, Math.Max(0, dauerSekunden)),
					angehalten, folge, zeit.GetTimestamp());
		}
		Geaendert?.Invoke();
		return true;
	}

	/// <summary>Uhr entfernen (z. B. beim Zuruecksetzen).</summary>
	public void Loeschen()
	{
		lock (_sperre) _stand = null;
		Geaendert?.Invoke();
	}

	/// <summary>Aktueller Stand mit herunter gezaehlter Restzeit.</summary>
	public ZielUhrAnzeige Anzeige()
	{
		Stand? s;
		lock (_sperre) s = _stand;
		if (s is null)
			return new ZielUhrAnzeige(ZielUhrPhase.Keine, 0, 0, 0, false, false);

		var vergangen = zeit.GetElapsedTime(s.Empfangen);
		var rest = s.Angehalten ? s.Rest : Math.Max(0, s.Rest - (int)Math.Floor(vergangen.TotalSeconds));
		var sichtbar = vergangen <= Verbindungsfrist;
		return new ZielUhrAnzeige(s.Phase, s.Durchgang, s.Dauer, rest, s.Angehalten, sichtbar);
	}

	/// <summary>Folgenummer der letzten Meldung (fuer GET).</summary>
	public long Folge
	{
		get { lock (_sperre) return _stand?.Folge ?? 0; }
	}
}
