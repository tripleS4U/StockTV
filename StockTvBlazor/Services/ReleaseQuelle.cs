using System.Reflection;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Services;

/// <summary>
/// GitHub-Repository, aus dem Updates kommen (besitzer/repository). Wird beim Bauen über die
/// MSBuild-Eigenschaft <c>StockTvRepository</c> festgelegt (Release-Pipeline: das Repository, aus dem
/// gebaut wird) – so bieten Geräte mit einem Build aus einem Fork dessen Releases an, nicht die des Originals.
/// </summary>
public static partial class ReleaseQuelle
{
	public const string Standard = "Trawacho/StockTV";

	public static string Repository { get; } = Bestimmen(
		typeof(ReleaseQuelle).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(a => a.Key == "StockTvRepository")?.Value);

	public static string LatestReleaseApiUrl => $"https://api.github.com/repos/{Repository}/releases/latest";

	public static string ReleasesSeite => $"https://github.com/{Repository}/releases";

	/// <summary>Nur „besitzer/name“ mit erlaubten Zeichen – sonst der Standard (landet in einer URL).</summary>
	public static string Bestimmen(string? wert) =>
		!string.IsNullOrWhiteSpace(wert) && GueltigesMuster().IsMatch(wert.Trim()) ? wert.Trim() : Standard;

	[GeneratedRegex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")]
	private static partial Regex GueltigesMuster();
}
