using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceZielAnsichtTests
{
	[Fact]
	public async Task Kacheln_nur_im_Ziel_Modus_mit_Seitenwechsel_klassisch_bleibt_Vorgabe()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		Assert.Equal(GameSettings.ZielAnsichtArt.Klassisch, new GameSettings().ZielAnsicht);

		service.CurrentSettings.Game.CurrentModus = GameSettings.Modus.Ziel2;
		string? navigiert = null;
		service.OnNavigationRequested += url => navigiert = url;

		service.SetZielAnsicht(GameSettings.ZielAnsichtArt.Kacheln);
		Assert.Equal("/ziel-kacheln", navigiert);

		service.SetZielAnsicht(GameSettings.ZielAnsichtArt.Klassisch);
		Assert.Equal("/ziel", navigiert);

		service.CurrentSettings.Game.CurrentModus = GameSettings.Modus.Turnier;
		navigiert = null;
		service.SetZielAnsicht(GameSettings.ZielAnsichtArt.Kacheln);
		Assert.Null(navigiert);   // im Turnier keine Navigation
		service.SetZielAnsicht(GameSettings.ZielAnsichtArt.Klassisch);
	}
}
