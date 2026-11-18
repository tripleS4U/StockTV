using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

/// <summary>Eine Kachel: ein Teil des Durchgangs (Maß Mitte, Stockschießen, Maß hinten, Kombinieren).</summary>
/// <param name="Zustand">„fertig“, „laeuft“ oder „offen“ (CSS-Klasse).</param>
public sealed record ZielKachel(int Nummer, string Name, string Summe, IReadOnlyList<bool> Versuche, string Zustand);

/// <summary>
/// Seite <c>/ziel-kacheln</c>: vier Kacheln, Aufgabe des nächsten Versuchs und Wertungsuhr. Die Eingabe läuft wie auf
/// <c>/ziel</c> über <see cref="ZielService"/>; die Uhr kommt aus <see cref="ZielUhrService"/> (nur von der Zentrale).
/// Solange eine Uhr zu sehen ist, meldet ein Sekundentakt Änderungen an die Seite.
/// </summary>
public sealed class ZielKachelnViewModel : IPageViewModel
{
	private readonly ZielService _zielService;
	private readonly SettingsService _settingsService;
	private readonly ZielUhrService _uhr;
	private readonly ZielBewerb _bewerb;
	private readonly Timer _takt;
	private bool _isDemoMode;
	private bool _disposed;

	public ZielKachelnViewModel(ZielService zielService, SettingsService settingsService, ZielUhrService uhr)
	{
		_zielService = zielService;
		_settingsService = settingsService;
		_uhr = uhr;
		_bewerb = zielService.CurrentZielBewerb;
		_bewerb.OnZielBewerbChanged += Geaendert;
		_zielService.OnGlobalRefresh += Geaendert;
		_settingsService.OnSettingsChanged += Geaendert;
		_uhr.Geaendert += Geaendert;
		_takt = new Timer(_ => { if (_uhr.Anzeige().Sichtbar || _uhrWarSichtbar) Geaendert(); }, null, 1000, 1000);
	}

	private bool _uhrWarSichtbar;

	public event Action? OnViewModelChanged;

	private void Geaendert()
	{
		if (_disposed) return;
		_uhrWarSichtbar = _uhr.Anzeige().Sichtbar;
		OnViewModelChanged?.Invoke();
	}

	public void EnableDemoMode() => _isDemoMode = true;

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_takt.Dispose();
		_bewerb.OnZielBewerbChanged -= Geaendert;
		_zielService.OnGlobalRefresh -= Geaendert;
		_settingsService.OnSettingsChanged -= Geaendert;
		_uhr.Geaendert -= Geaendert;
	}

	private int VersucheProTeil => _isDemoMode ? 6 : Math.Max(1, _settingsService.CurrentSettings.Game.MaxKehrenProSpiel);

	/// <summary>Versuche je Teil in der laufenden Runde (Demo: Teil 1 voll, Teil 2 halb).</summary>
	private IReadOnlyList<IReadOnlyList<int>> Teile()
	{
		if (_isDemoMode)
			return [[4, 4, 4, 4, 4, 4], [5, 10, 2], [], []];
		var s = _bewerb.CreateSnapshot();
		return [s.MassenVorne, s.Schiessen, s.MassenSeite, s.Kombinieren];
	}

	/// <summary>Index des laufenden Teils (erster nicht voller), 4 = alle Teile fertig.</summary>
	private int LaufenderTeil(IReadOnlyList<IReadOnlyList<int>> teile)
	{
		for (var i = 0; i < teile.Count; i++)
			if (teile[i].Count < VersucheProTeil) return i;
		return teile.Count;
	}

	public IReadOnlyList<ZielKachel> Kacheln
	{
		get
		{
			var teile = Teile();
			var laufend = LaufenderTeil(teile);
			return teile.Select((v, i) => new ZielKachel(
				i + 1,
				ZielAufgaben.Namen[i],
				v.Count == 0 ? "–" : v.Sum().ToString(),
				Enumerable.Range(0, VersucheProTeil).Select(n => n < v.Count).ToList(),
				i < laufend ? "fertig" : i == laufend ? "laeuft" : "offen")).ToList();
		}
	}

	/// <summary>„Versuch 4 von 6 · Zielstock D“ bzw. „alle Versuche eingetragen“.</summary>
	public string Aufgabe
	{
		get
		{
			var teile = Teile();
			var t = LaufenderTeil(teile);
			if (t >= teile.Count) return "alle Versuche eingetragen";
			var versuch = teile[t].Count + 1;
			return $"Versuch {versuch} von {VersucheProTeil} · {ZielAufgaben.Aufgabe(t, versuch, VersucheProTeil)}";
		}
	}

	public string ErlaubteWerte
	{
		get
		{
			var t = LaufenderTeil(Teile());
			return t >= 4 ? "" : $"erlaubt: {ZielAufgaben.ErlaubteWerte(t)}";
		}
	}

	public string SpielerName => _isDemoMode ? DemoData.ZielSpielerName : _bewerb.Spielername;
	public bool HatSpielerName => !string.IsNullOrEmpty(SpielerName);

	/// <summary>Im Modus Ziel2: „Runde 1“/„Runde 2“, sonst leer.</summary>
	public string Runde => !_isDemoMode && _settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel2
		? $"Runde {_bewerb.CreateSnapshot().Durchgang}"
		: "";

	public string Gesamt => _isDemoMode ? (24 + 17).ToString() : _bewerb.GesamtSumme.ToString();

	public string Eingabe => _isDemoMode ? "" : (_zielService.InputValue < 0 ? "" : _zielService.InputValue.ToString());

	public string Letzter => _isDemoMode ? "2" : _zielService.InputValue == -1 && _bewerb.AnzahlVersuche() > 0
		? _bewerb.LetzterVersuch().ToString()
		: "";

	public bool Ungueltig => !_isDemoMode && _zielService.InvalidInput;

	#region Uhr

	private ZielUhrAnzeige UhrStand => _isDemoMode
		? new ZielUhrAnzeige(ZielUhrPhase.Durchgang, 2, 18 * 60, 11 * 60 + 42, false, true)
		: _uhr.Anzeige();

	/// <summary>Ohne Verbindung keine Uhr.</summary>
	public bool UhrSichtbar => UhrStand.Sichtbar;

	public string UhrPhase => UhrStand switch
	{
		{ Phase: ZielUhrPhase.Einspielen } => "Einspielen",
		{ Phase: ZielUhrPhase.Durchgang, Durchgang: > 0 } u => $"Durchgang {u.Durchgang}",
		{ Phase: ZielUhrPhase.Durchgang } => "Durchgang",
		_ => "",
	};

	public string UhrText => UhrStand.Abgelaufen ? "Zeit abgelaufen" : UhrStand.RestText;

	/// <summary>CSS-Klasse: „halt“ (angehalten), „aus“ (0:00), „blink“ (letzte 30 s), „warn“ (letzte 2 min), sonst leer.</summary>
	public string UhrKlasse => UhrStand switch
	{
		{ Abgelaufen: true } => "aus",
		{ Angehalten: true } => "halt",
		{ RestSekunden: <= 30 } => "blink",
		{ RestSekunden: <= 120 } => "warn",
		_ => "",
	};

	#endregion

	public string GetTableLayoutCssVariables() => _settingsService.CurrentSettings.UI.TableLayout.ToCssVariables();
}
