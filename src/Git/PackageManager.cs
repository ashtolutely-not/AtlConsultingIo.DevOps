

using CliWrap;
using CliWrap.Buffered;

using System.Diagnostics;

namespace AtlConsultingIo.DevOps;
internal static class PackageManager
{
	private static readonly string _packageSource = "github";
    public static async Task CreateNugetPackage( this AtlProject directory, string? buildProfileName = default, string? versionSuffix = default)
	{
		var _profile = string.IsNullOrEmpty(buildProfileName) ? "Release" : buildProfileName;
		
		var args = $"pack --configuration {_profile.SurroundWithDoubleQuotes()}";
		if( !string.IsNullOrEmpty(versionSuffix))
			args += $" --version-suffix {versionSuffix.SurroundWithDoubleQuotes()}";

		var cmdResult = await
				Cli.Wrap("dotnet")
				.WithWorkingDirectory(directory.PhysicalDirectory.FullName)
				.WithArguments(args)
				.ExecuteBufferedAsync();

		Console.WriteLine(cmdResult.StandardOutput);
	}

	public static async Task PushLatestNugetPackage(this AtlProject directory)
	{
		var token = GetGhPackagesToken();
		if( string.IsNullOrEmpty(token)) return;

		var bin = directory.PhysicalDirectory.EnumerateDirectories("bin", Utils.RecursionOptions)?.FirstOrDefault();
		if( bin is null ) return;

		var release = bin.GetDirectories("Release").FirstOrDefault();
		if( release is null ) return;

		var nupkg = release.GetFiles("*.nupkg")?.OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
		if( nupkg is null ) return;

		Console.WriteLine("Pushing package to GitHub Packages...");

		var args = $"nuget push {nupkg.FullName.SurroundWithDoubleQuotes()}";
		args += $" --api-key {token.SurroundWithDoubleQuotes()}";
		args += $" --source {_packageSource.SurroundWithDoubleQuotes()}";

		var cmdResult = await
				Cli.Wrap("dotnet")
				.WithWorkingDirectory(directory.PhysicalDirectory.FullName)
				.WithArguments(args)
				.ExecuteBufferedAsync();

		Console.WriteLine(cmdResult.StandardOutput);
	}

	public static async Task PushNugetPackage(this AtlProject directory, string packageFileName)
	{
		var token =GetGhPackagesToken();
		if( string.IsNullOrEmpty(token)) return;

		var info = new DirectoryInfo( directory.PhysicalDirectory.FullName );
		if( !info.Exists ) return;

		var nupkg = info.GetFiles( packageFileName , SearchOption.AllDirectories )?.FirstOrDefault();
		if( nupkg is null ) return;

		Console.WriteLine("Pushing package to GitHub Packages...");
		var cmdResult = await
				Cli.Wrap("dotnet")
				.WithWorkingDirectory(directory.PhysicalDirectory.FullName)
				.WithArguments($"nuget push {nupkg.FullName} --api-key {token} --source github")
				.WithValidation(CommandResultValidation.None)
				.ExecuteBufferedAsync();
	}
	private static string GetGhPackagesToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(CommandParams.GitHubTokensPath, ".packages-token");

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
