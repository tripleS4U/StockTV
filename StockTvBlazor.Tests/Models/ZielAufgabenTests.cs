using StockTvBlazor.Models;

namespace StockTvBlazor.Tests.Models;

public class ZielAufgabenTests
{
	[Theory]
	[InlineData(0, 1, 6, "Mitte")]
	[InlineData(1, 4, 6, "Zielstock D")]
	[InlineData(2, 3, 6, "hinten links")]
	[InlineData(2, 4, 6, "hinten rechts")]
	[InlineData(3, 1, 6, "Ablenken · Zielstock A")]
	[InlineData(3, 6, 6, "hinten rechts · Zielstock F")]
	[InlineData(1, 4, 4, "Zielstock D")]
	[InlineData(2, 3, 4, "hinten rechts")]
	[InlineData(3, 1, 4, "Aufgeben · Zielstock G")]
	[InlineData(1, 2, 5, "Stockschießen")]
	[InlineData(1, 7, 6, "")]
	public void Aufgabe_je_Versuch_wie_in_StockSuite(int teil, int versuch, int versuche, string erwartet) =>
		Assert.Equal(erwartet, ZielAufgaben.Aufgabe(teil, versuch, versuche));

	[Fact]
	public void Erlaubte_Werte() 
	{
		Assert.Equal("0 · 2 · 5 · 10", ZielAufgaben.ErlaubteWerte(1));
		Assert.Equal("0 · 2 · 4 · 6 · 8 · 10", ZielAufgaben.ErlaubteWerte(3));
	}
}
