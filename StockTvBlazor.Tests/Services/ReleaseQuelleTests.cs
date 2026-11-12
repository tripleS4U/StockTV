using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class ReleaseQuelleTests
{
	[Theory]
	[InlineData("tripleS4U/StockTV", "tripleS4U/StockTV")]
	[InlineData("  Trawacho/StockTV ", "Trawacho/StockTV")]
	[InlineData("a.b-c_d/repo.name", "a.b-c_d/repo.name")]
	public void Gueltige_Angaben_werden_uebernommen(string wert, string erwartet) =>
		Assert.Equal(erwartet, ReleaseQuelle.Bestimmen(wert));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("nurbesitzer")]
	[InlineData("a/b/c")]
	[InlineData("evil/repo?x=1")]
	[InlineData("a b/c")]
	public void Ungueltige_Angaben_fallen_auf_den_Standard_zurueck(string? wert) =>
		Assert.Equal(ReleaseQuelle.Standard, ReleaseQuelle.Bestimmen(wert));

	[Fact]
	public void Ohne_Build_Eigenschaft_gilt_das_Original_und_die_Urls_passen()
	{
		// Im Test-Build ist StockTvRepository nicht gesetzt -> Vorgabe aus dem csproj.
		Assert.Equal("Trawacho/StockTV", ReleaseQuelle.Repository);
		Assert.Equal("https://api.github.com/repos/Trawacho/StockTV/releases/latest", ReleaseQuelle.LatestReleaseApiUrl);
		Assert.Equal("https://github.com/Trawacho/StockTV/releases", ReleaseQuelle.ReleasesSeite);
	}
}
