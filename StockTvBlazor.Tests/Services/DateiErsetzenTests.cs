using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class DateiErsetzenTests : IDisposable
{
	private readonly string _ordner = Directory.CreateTempSubdirectory("stocktv-ersetzen-").FullName;

	public void Dispose() => Directory.Delete(_ordner, recursive: true);

	[Fact]
	public async Task Ersetzt_die_Zieldatei()
	{
		var ziel = Path.Combine(_ordner, "a.json");
		var temp = ziel + ".tmp";
		File.WriteAllText(ziel, "alt");
		File.WriteAllText(temp, "neu");

		await DateiErsetzen.AusTempAsync(temp, ziel);

		Assert.Equal("neu", File.ReadAllText(ziel));
		Assert.False(File.Exists(temp));
	}

	[Fact]
	public async Task Wartet_bis_ein_Leser_die_Datei_freigibt()
	{
		var ziel = Path.Combine(_ordner, "b.json");
		var temp = ziel + ".tmp";
		File.WriteAllText(ziel, "alt");
		File.WriteAllText(temp, "neu");

		// Ein Leser ohne Loesch-Freigabe (wie File.ReadAllText) blockiert das Ersetzen unter Windows kurz.
		var leser = new FileStream(ziel, FileMode.Open, FileAccess.Read, FileShare.Read);
		var ersetzen = DateiErsetzen.AusTempAsync(temp, ziel);
		await Task.Delay(120);
		await leser.DisposeAsync();
		await ersetzen;

		Assert.Equal("neu", File.ReadAllText(ziel));
	}
}
