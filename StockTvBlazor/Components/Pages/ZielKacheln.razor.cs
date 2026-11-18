using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

/// <summary>
/// Zielbewerb mit vier Kacheln, Aufgabe des nächsten Versuchs und Wertungsuhr. Wird über die Ziel-Ansicht „Kacheln“
/// (REST, StockSuite) statt <c>/ziel</c> gezeigt; <c>/ziel</c> bleibt für die alte StockApp.
/// </summary>
public partial class ZielKacheln : MirrorableGamePageBase<ZielKachelnViewModel>
{
	[Inject] private ZielService _zielService { get; set; } = default!;

	[Inject] private ZielKachelnViewModel _viewModel { get; set; } = default!;

	protected override ZielKachelnViewModel ViewModel => _viewModel;

	protected override IGameInputService GameService => _zielService;
}
