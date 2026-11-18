using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class ZielUhrServiceTests
{
	/// <summary>Zeitgeber zum Weiterdrehen (monoton, ohne Tageszeit).</summary>
	private sealed class TestZeit : TimeProvider
	{
		private long _ticks;
		public override long TimestampFrequency => TimeSpan.TicksPerSecond;
		public override long GetTimestamp() => _ticks;
		public void Weiter(TimeSpan dauer) => _ticks += dauer.Ticks;
	}

	[Fact]
	public void Ohne_Meldung_keine_Uhr()
	{
		var uhr = new ZielUhrService(new TestZeit());
		Assert.False(uhr.Anzeige().Sichtbar);
		Assert.Equal(ZielUhrPhase.Keine, uhr.Anzeige().Phase);
	}

	[Fact]
	public void Zaehlt_herunter_und_bleibt_bei_null_stehen()
	{
		var zeit = new TestZeit();
		var uhr = new ZielUhrService(zeit);
		uhr.Setzen(ZielUhrPhase.Durchgang, 1, 18 * 60, 18 * 60, false, 1);

		zeit.Weiter(TimeSpan.FromSeconds(61.5));
		var a = uhr.Anzeige();
		Assert.Equal(18 * 60 - 61, a.RestSekunden);
		Assert.Equal("16:59", a.RestText);
		Assert.False(a.Abgelaufen);

		uhr.Setzen(ZielUhrPhase.Durchgang, 1, 18 * 60, 5, false, 2);   // Wiederholung der Zentrale gleicht ab
		zeit.Weiter(TimeSpan.FromSeconds(10));
		Assert.True(uhr.Anzeige().Abgelaufen);
		Assert.Equal("00:00", uhr.Anzeige().RestText);
	}

	[Fact]
	public void Angehalten_bleibt_stehen()
	{
		var zeit = new TestZeit();
		var uhr = new ZielUhrService(zeit);
		uhr.Setzen(ZielUhrPhase.Einspielen, 1, 480, 300, true, 1);
		zeit.Weiter(TimeSpan.FromSeconds(30));
		Assert.Equal(300, uhr.Anzeige().RestSekunden);
		Assert.True(uhr.Anzeige().Angehalten);
	}

	[Fact]
	public void Ohne_Verbindung_verschwindet_die_Uhr()
	{
		var zeit = new TestZeit();
		var uhr = new ZielUhrService(zeit);
		uhr.Setzen(ZielUhrPhase.Durchgang, 2, 1080, 1080, false, 1);
		zeit.Weiter(ZielUhrService.Verbindungsfrist - TimeSpan.FromSeconds(1));
		Assert.True(uhr.Anzeige().Sichtbar);
		zeit.Weiter(TimeSpan.FromSeconds(2));
		Assert.False(uhr.Anzeige().Sichtbar);

		uhr.Setzen(ZielUhrPhase.Durchgang, 2, 1080, 900, false, 2);   // Meldung kommt wieder
		Assert.True(uhr.Anzeige().Sichtbar);
	}

	[Fact]
	public void Aeltere_Meldung_wird_ignoriert_Phase_keine_loescht()
	{
		var uhr = new ZielUhrService(new TestZeit());
		var geaendert = 0;
		uhr.Geaendert += () => geaendert++;

		Assert.True(uhr.Setzen(ZielUhrPhase.Durchgang, 3, 1080, 1000, false, 5));
		Assert.False(uhr.Setzen(ZielUhrPhase.Einspielen, 1, 480, 480, false, 4));
		Assert.Equal(3, uhr.Anzeige().Durchgang);
		Assert.Equal(1, geaendert);

		uhr.Setzen(ZielUhrPhase.Keine, 0, 0, 0, false, 6);
		Assert.False(uhr.Anzeige().Sichtbar);
	}

	[Fact]
	public void Restzeit_wird_auf_die_Dauer_begrenzt()
	{
		var uhr = new ZielUhrService(new TestZeit());
		uhr.Setzen(ZielUhrPhase.Durchgang, 1, 600, 9999, false, 1);
		Assert.Equal(600, uhr.Anzeige().RestSekunden);
	}
}
