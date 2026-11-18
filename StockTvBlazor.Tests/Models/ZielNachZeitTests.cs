using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Api;
using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Models;

public class ZielNachZeitTests
{
	private static async Task<ZielBewerb> BewerbAsync(GameSettings.Modus modus = GameSettings.Modus.Ziel)
	{
		var settings = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await settings.InitializeAsync();
		settings.CurrentSettings.Game.CurrentModus = modus;
		settings.CurrentSettings.Game.MaxKehrenProSpiel = 6;
		return new ZielBewerb(settings);
	}

	[Fact]
	public async Task Markierung_je_Teil_und_Versuch()
	{
		var z = await BewerbAsync();
		foreach (var _ in Enumerable.Range(0, 6)) z.AddVersuch(8);   // Teil 1 voll
		z.AddVersuch(5);
		z.AddVersuch(10, nachZeit: true);   // Teil 2, Versuch 2

		Assert.Empty(z.NachZeit(0));
		Assert.Equal([2], z.NachZeit(1));

		var dto = GameDtoFactory.Ziel(z);
		Assert.Equal([2], dto.Disziplinen[1].NachZeit);
		Assert.Empty(dto.Disziplinen[0].NachZeit!);
	}

	[Fact]
	public async Task Loeschen_Zuruecksetzen_und_Spielstand()
	{
		var z = await BewerbAsync();
		z.AddVersuch(8, nachZeit: true);
		z.DeleteLastVersuch();
		Assert.Empty(z.NachZeit(0));

		z.AddVersuch(8);
		z.AddVersuch(6, nachZeit: true);
		var stand = z.CreateSnapshot();
		Assert.Equal([1], stand.NachZeit);

		var neu = await BewerbAsync();
		neu.RestoreFrom(stand);
		Assert.Equal([2], neu.NachZeit(0));

		neu.Reset();
		Assert.Empty(neu.NachZeit(0));
	}

	[Fact]
	public async Task Ungueltiger_Wert_wird_nicht_markiert()
	{
		var z = await BewerbAsync();
		Assert.False(z.AddVersuch(5, nachZeit: true));   // 5 ist bei Maß Mitte nicht erlaubt
		Assert.Empty(z.NachZeit(0));
	}

	[Fact]
	public async Task Ziel2_zweite_Runde_beginnt_ohne_Markierungen()
	{
		var z = await BewerbAsync(GameSettings.Modus.Ziel2);
		foreach (var i in Enumerable.Range(0, 24)) z.AddVersuch(i == 23 ? 8 : (i >= 6 && i < 12 ? 10 : 8), nachZeit: i == 23);
		Assert.Equal([6], z.NachZeit(3));
		z.AddVersuch(8);   // erster Versuch der zweiten Runde
		Assert.Empty(z.NachZeit(3));
	}
}
