using CliWrap;
using CliWrap.Buffered;

using Microsoft.IdentityModel.Protocols;

namespace AtlConsultingIo.DevOps;
internal static class BuildProjectCommands
{
    public static readonly string GitHubPackageSource = "github";

    public static void UpdateNamespaces( BuildProject project , NamespaceUpdateParams @params )
    {
        var srcFiles = GetFilesToUpdate( project.ProjectDirectoryInfo, @params.IgnoreTopLevelFiles , @params.ChildDirectoryFilter );
        if ( !srcFiles.Any() )
        {
            Console.WriteLine("No files to update found.  Namespaces were not changed.");
            return;
        }

        srcFiles.ForEach( srcFile => UpdateFileNamespace( srcFile, NamespaceStatement(@params.CurrentValue), NamespaceStatement(@params.NewValue) ) );
    }

    static string NamespaceStatement( string @namespace ) => $"namespace {@namespace};";
    static void UpdateFileNamespace( FileInfo file, string searchValue, string replaceValue )
    {
        var lines = File.ReadAllLines( file.FullName ).ToList();
        var replaceIndex = lines.FindIndex( ln => ln.StartsWith( searchValue ));
        if( replaceIndex < 0 )
            return;

        lines[replaceIndex ] = replaceValue;    
        File.WriteAllLines( file.FullName, lines );
    }
    static List<FileInfo> GetFilesToUpdate(
        DirectoryInfo projectDirectory ,
        bool ignoreTopDirectoryFiles ,
        Func<string , bool>? childDirectoryFilter )
    {
        var files = projectDirectory.GetFiles("*.cs")?.ToList() ?? new List<FileInfo>();
        if( ignoreTopDirectoryFiles ) 
            files.Clear();

        var childDirs = projectDirectory.GetDirectories();
        foreach ( var _dir in childDirs )
            if ( childDirectoryFilter is null || childDirectoryFilter( _dir.Name ) )
                if ( _dir.GetFiles( "*.cs" ) is FileInfo[] _files )
                    files.AddRange( _files );

        return files;
    }
    public static async Task PushLocalCommits( BuildProject project )
    {
        var cmd = project.InitializeGhCommand().WithArguments("push");
        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }
    public static async Task AddCommitFiles( BuildProject project )
    {
        await EnsureSafeDirectory( project );
        var cmd = project.InitializeGhCommand().WithArguments("add -A");
        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }
    public static async Task CreateLocalCommitFromFile( BuildProject project )
    {
        var commitFile = project.GetCommitMessageFile();
        if ( commitFile is null )
            throw new Exception( MissingCommitFileError( project ) );

        var cmd = project.InitializeGhCommand().WithArguments($"commit -F { commitFile.FullName.SurroundWithDoubleQuotes() }");
        var result = await cmd.ExecuteBufferedAsync();

        result.PrintErrorOutput();
    }
    public static async Task CreateLocalCommitFromMessage( BuildProject project , string commitMessage )
    {
        var cmd = project.InitializeGhCommand().WithArguments($"commit -m { commitMessage.SurroundWithDoubleQuotes() }");
        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }

    public static async Task CreateNewBuild( BuildProject project , BuildProfile profile , bool updateVersion , bool validateResult )
    {
        var args = $"build --configuration { profile.ToString() } ";
        if ( updateVersion )
            project.UpdateProjectWithNextVersion();

        var cmd = project.InitializeDotNetCommand( validateResult ).WithArguments( args );
        var result = await cmd.ExecuteBufferedAsync();

        result.PrintErrorOutput();

    }
    public static async Task CreateLocalNugetPackage( BuildProject project , BuildProfile profile , bool validateResult )
    {
        var args = $"pack --configuration { profile.ToString().SurroundWithDoubleQuotes() }";
        if ( profile.Equals( BuildProfile.Debug ) )
            args += " --include-symbols";

        var cmd = project.InitializeDotNetCommand( validateResult ).WithArguments( args );
        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }
    public static void CopyNugetFileToLocalPackages( BuildProject project, BuildProfile profile, string version )
    {
        var fileInfo = project.GetNugetFile( profile, version );
        var targetPath = Path.Combine( CommandParams.DirectoryPaths.LocalPackageDirectory, fileInfo.Name );

        File.WriteAllBytes( targetPath, File.ReadAllBytes( fileInfo.FullName ));
    }
    public static async Task PushPackageToGh( BuildProject project , BuildProfile profile , string version , bool validateResult )
    {
        var token = GetGhPackagesToken();
        if ( string.IsNullOrEmpty( token ) )
            throw new Exception( $"Cannot locate GH Package Token. Location: {Path.Combine( CommandParams.GitHubTokensPath , ".packages-token" )}" );

        var nupkg = project.GetNugetFile( profile , version );
        var args = $"nuget push { nupkg.FullName.SurroundWithDoubleQuotes() } --api-key {token.SurroundWithDoubleQuotes() } --source { GitHubPackageSource }";

        Console.WriteLine( "Pushing package to GitHub Packages..." );
        var cmd = project.InitializeDotNetCommand( validateResult ).WithArguments( args );

        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }
    static string MissingCommitFileError( BuildProject project )
        => $"Could not find commit message text file. Location Searched: {Path.Combine( project.ProjectDirectoryInfo.FullName , BuildProject.CommitFileName )}";
    public static string GetGhAccessToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(userDir, ".config", "ghtokens", ".project-builder-token");

        return File.Exists( path ) ? File.ReadAllText( path ) : string.Empty;
    }
    private static string GetGhPackagesToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(CommandParams.GitHubTokensPath, ".packages-token");

        return File.Exists( path ) ? File.ReadAllText( path ) : string.Empty;
    }

    public static Command InitializeDotNetCommand( this BuildProject project , bool withValidation = true )
    {
        var cmd = Cli.Wrap( "dotnet" )
                    .WithWorkingDirectory( project.ProjectDirectoryInfo.FullName );

        return withValidation ? cmd : cmd.WithValidation( CommandResultValidation.None );
    }
    public static Command InitializeGhCommand( this BuildProject project , bool withValidation = true )
    {
        var cmd = Cli.Wrap( "git" )
                    .WithWorkingDirectory( project.ProjectDirectoryInfo.FullName );

        return withValidation ? cmd : cmd.WithValidation( CommandResultValidation.None );
    }

    static async Task EnsureSafeDirectory( BuildProject project )
    {
        var cmd = project.InitializeGhCommand( ).WithArguments($@"config --global --add safe.directory { project.ProjectDirectoryInfo.FullName }");
        var result = await cmd.ExecuteBufferedAsync();
        result.PrintErrorOutput();
    }
    public static void PrintErrorOutput( this BufferedCommandResult result )
    {
        if ( !string.IsNullOrEmpty( result.StandardError ) )
            Console.WriteLine( result.StandardError );
    }
}


public class NamespaceUpdateParams
{
    public string CurrentValue { get; set; } = String.Empty;
    public string NewValue { get; set; } = String.Empty;

    public bool IgnoreTopLevelFiles { get; set; } 
    public Func<string,bool>? ChildDirectoryFilter { get; set; }
}