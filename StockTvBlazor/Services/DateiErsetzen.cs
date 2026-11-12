namespace StockTvBlazor.Services;

/// <summary>
/// Ersetzt eine Datei durch eine fertig geschriebene temporaere Datei und wiederholt das kurz, wenn die Zieldatei
/// gerade offen ist.
/// </summary>
/// <remarks>
/// Unter Windows scheitert <see cref="File.Move(string,string,bool)"/> mit einer <see cref="IOException"/>, solange ein
/// anderes Programm die Zieldatei ohne Loesch-Freigabe geoeffnet hat - ein Virenscanner, ein Sicherungsprogramm oder
/// jemand, der die Datei gerade liest (die E2E-Tests lesen <c>stocktv.config.json</c> alle 150 ms). Ohne Wiederholung
/// ging die Aenderung dann verloren; im Log stand nur "Fehler beim Speichern".
/// </remarks>
public static class DateiErsetzen
{
	private const int Versuche = 10;
	private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(50);

	public static async Task AusTempAsync(string tempPfad, string zielPfad, CancellationToken ct = default)
	{
		for (var versuch = 1; ; versuch++)
		{
			try
			{
				File.Move(tempPfad, zielPfad, overwrite: true);
				return;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && versuch < Versuche)
			{
				await Task.Delay(Pause, ct);
			}
		}
	}
}
