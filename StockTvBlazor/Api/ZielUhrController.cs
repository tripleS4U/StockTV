using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>Stand der Wertungsuhr (<c>PUT/GET /api/v1/ziel/uhr</c>). Enums als Zahl.</summary>
/// <param name="Phase">0 = keine Uhr, 1 = Einspielen, 2 = Durchgang.</param>
/// <param name="Durchgang">Nummer des Durchgangs (1 …), bei Einspielen der folgende.</param>
/// <param name="DauerSekunden">Laenge der Phase.</param>
/// <param name="RestSekunden">Restzeit beim Senden.</param>
/// <param name="Angehalten">Uhr steht (Unterbrechung).</param>
/// <param name="Folge">Fortlaufende Nummer der Zentrale; aeltere Meldungen werden ignoriert.</param>
public sealed record ZielUhrDto(ZielUhrPhase Phase, int Durchgang, int DauerSekunden, int RestSekunden, bool Angehalten, long Folge);

/// <summary>Ansicht der Modi Ziel/Ziel2 (<c>PUT/GET /api/v1/ziel/ansicht</c>): 0 = klassisch (/ziel), 1 = Kacheln mit Uhr.</summary>
public sealed record ZielAnsichtDto(GameSettings.ZielAnsichtArt Ansicht);

/// <summary>
/// Wertungsuhr und Ansicht fuer den Zielwettbewerb - nur REST, kein NetMQ-Gegenstueck (die alte StockApp kennt beides
/// nicht und bleibt bei der klassischen Seite). Die Uhr kommt allein von der Zentrale: ohne Verbindung keine Uhr.
/// </summary>
[ApiController]
[Route("api/v1/ziel")]
[Produces("application/json")]
public sealed class ZielUhrController(
	ZielUhrService uhr,
	SettingsService settingsService,
	GameCommandQueue commands,
	ILogger<ZielUhrController> logger) : ControllerBase
{
	/// <summary>Aktueller Stand der Uhr mit herunter gezaehlter Restzeit.</summary>
	[HttpGet("uhr")]
	[ProducesResponseType<ZielUhrDto>(StatusCodes.Status200OK)]
	public ActionResult<ZielUhrDto> GetUhr()
	{
		var a = uhr.Anzeige();
		return Ok(new ZielUhrDto(a.Sichtbar ? a.Phase : ZielUhrPhase.Keine, a.Durchgang, a.DauerSekunden, a.RestSekunden, a.Angehalten, uhr.Folge));
	}

	/// <summary>Setzt die Uhr (Phase 0 = Uhr aus). Wird von der Zentrale regelmaessig wiederholt.</summary>
	[HttpPut("uhr")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetUhr([FromBody] ZielUhrDto? request)
	{
		if (request is null || !Enum.IsDefined(request.Phase))
			return BadRequest(new ActionResponse("InvalidRequest", "Phase fehlt oder ist unbekannt.", null));
		if (request.DauerSekunden is < 0 or > 6 * 3600 || request.RestSekunden < 0 || request.Durchgang is < 0 or > 99)
			return BadRequest(new ActionResponse("InvalidRequest", "Dauer, Restzeit oder Durchgang liegt ausserhalb des erlaubten Bereichs.", null));

		var neu = uhr.Setzen(request.Phase, request.Durchgang, request.DauerSekunden, request.RestSekunden, request.Angehalten, request.Folge);
		return Ok(new ActionResponse(neu ? "Ok" : "Veraltet", neu ? "Uhr wurde gesetzt." : "Aeltere Meldung ignoriert.", null));
	}

	[HttpGet("ansicht")]
	[ProducesResponseType<ZielAnsichtDto>(StatusCodes.Status200OK)]
	public ActionResult<ZielAnsichtDto> GetAnsicht() => Ok(new ZielAnsichtDto(settingsService.CurrentSettings.Game.ZielAnsicht));

	/// <summary>Stellt die Ansicht um; im Modus Ziel/Ziel2 wechselt die Anzeige sofort die Seite.</summary>
	[HttpPut("ansicht")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetAnsicht([FromBody] ZielAnsichtDto? request)
	{
		if (request is null || !Enum.IsDefined(request.Ansicht))
			return BadRequest(new ActionResponse("InvalidRequest", "Ansicht fehlt oder ist unbekannt.", null));
		logger.LogInformation("Ziel-Ansicht ueber REST: {Ansicht}", request.Ansicht);
		commands.Enqueue(() => settingsService.SetZielAnsicht(request.Ansicht));
		return Accepted(new ActionResponse("Ok", "Ansicht wurde angenommen.", null));
	}
}
