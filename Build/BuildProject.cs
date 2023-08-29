using System.Reflection;
using System;
using System.Xml;
using Microsoft.CodeAnalysis.Text;

namespace AtlConsultingIo.DevOps;

internal record BuildProject
{
    public const string CommitFileName = ".commit-msg";

    public const string MsBuildXmlNamespace = "ms";
    public const string MsBuildSchemaUrl = "http://schemas.microsoft.com/developer/msbuild/2003";
    public const string DefaultProjectVersion = "1.0.0";
    public DirectoryInfo ProjectDirectoryInfo { get; init; }
    public FileInfo ProjectFileInfo { get; init; }
    public FileInfo? SolutionFileInfo { get; init; }
    public XmlDocument CsProjectXml { get; set; } = new();

    //Source Generation Tools
    public Compilation? Compilation { get; set; }
    public CSharpSyntaxNode? SyntaxTree { get; set; }
    public CompilationUnitSyntax? SyntaxRoot { get; set; }

    public readonly ProjectId ProjectId = ProjectId.CreateNewId();
    public readonly VersionStamp VersionStamp = VersionStamp.Default;

    public readonly AdhocWorkspace Workspace = new AdhocWorkspace();
    private BuildProject( DirectoryInfo projectDirectory , FileInfo projectFile )
    {
        if ( !projectDirectory.Exists )
            throw new ArgumentException( "Project directory does not exist." );

        if ( !projectFile.Exists )
            throw new ArgumentException( "Project file does not exist." );

        ProjectDirectoryInfo = projectDirectory;
        ProjectFileInfo = projectFile;

        CsProjectXml = new();
        CsProjectXml.Load( ProjectFileInfo.FullName );
    }

    public XmlNamespaceManager NamespaceManager()
    {
        var mgr = new XmlNamespaceManager( CsProjectXml.NameTable );
        mgr.AddNamespace( MsBuildXmlNamespace , MsBuildSchemaUrl );
        return mgr;
    }
    public FileInfo? GetCommitMessageFile()
        => ProjectDirectoryInfo.EnumerateFiles( CommitFileName , SearchOption.AllDirectories ).FirstOrDefault();

    public void UpdateProjectWithNextVersion()
    {
        UpdateProjectFile( GetNextVersion() );
    }
    public void UpdateProjectFile( string version )
    {
        var csproj = CsProjectXml;
        var versionNode = csproj.SelectSingleNode("./Project/PropertyGroup/Version");
        if ( versionNode is null )
            AddVersionNode( version );
        else versionNode.InnerText = version;

        CsProjectXml.Save( ProjectFileInfo.FullName );
    }
    void AddVersionNode( string version )
    {
        var projectProperties = CsProjectXml.SelectNodes("./Project/PropertyGroup")?.Item(0);
        if ( projectProperties is not XmlNode _node )
            throw new Exception( "Cannot find project properties XML Node." );

        var versionNode = CsProjectXml.CreateElement("Version");
        versionNode.InnerText = version;

        _ = _node.AppendChild( versionNode );
    }

    public string GetCurrentVersion()
    {
        var versionNode = CsProjectXml.SelectSingleNode("./Project/PropertyGroup/Version");
        return versionNode is not XmlNode _node ? DefaultProjectVersion : _node.InnerText;
    }
    public string GetNextVersion()
    {
        var version = GetCurrentVersion(  );
        var parts = GetVersionParts( version );
        return string.Join( "." , parts.Major , parts.Minor , parts.Patch + 1 );
    }

    public async Task InitializeWorkspace()
    {
        var srcFiles = ProjectDirectoryInfo.GetFiles("*.cs", SearchOption.AllDirectories);
        string assemblyName = ProjectFileInfo.Name.Replace(".csproj","") ;
        Project project = Workspace.AddProject(
        ProjectInfo
                .Create( ProjectId, VersionStamp, assemblyName, assemblyName, LanguageNames.CSharp)
                .WithDefaultNamespace( assemblyName )
            );

        Compilation = CSharpCompilation.Create( assemblyName );
        foreach ( var file in srcFiles )
        {
            SourceText src = SourceText.From( File.ReadAllText( file.FullName ));
            TextLoader loader = TextLoader.From( TextAndVersion.Create( src, VersionStamp.Create(), file.FullName ));
            DocumentInfo info = DocumentInfo
                            .Create( DocumentId.CreateNewId( project.Id ), file.Name, loader: loader )
                            .WithFilePath( file.FullName);

            Document doc = Workspace.AddDocument( info );
            if ( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
                Compilation = Compilation.AddSyntaxTrees( _tree );
        }
    }



    public FileInfo GetNugetFile( BuildProfile profile , string version )
    {
        var directoryPath = Path.Combine( ProjectDirectoryInfo.FullName, "bin", profile.ToString() );
        var searchDirectory = new DirectoryInfo( directoryPath );
        if ( !searchDirectory.Exists )
            throw new InvalidOperationException( $"Cannot find project build directory for profile {profile.ToString()} (Path: {searchDirectory.FullName})" );

        var nugetFiles = GetNugetFiles( searchDirectory, SearchOption.TopDirectoryOnly );
        if ( !nugetFiles.Any() )
            throw new InvalidOperationException( $"Cannot find any nuget package files in the build directory for profile {profile.ToString()} (Path: {searchDirectory.FullName})" );

        var versionFile = nugetFiles.FirstOrDefault( x => x.Name.Contains( version ));
        if ( versionFile is null )
            throw new InvalidOperationException( $"Cannot find nuget package file with version {version} in the build directory for profile {profile.ToString()}" );

        return versionFile;
    }
    public static List<FileInfo> GetNugetFiles( DirectoryInfo directory , SearchOption searchOption )
    {
        var files = directory.GetFiles("*.nupkg", searchOption );
        return files is null ? new List<FileInfo>() : files.ToList();
    }
    private static (int Major, int Minor, int Patch) GetVersionParts( string version )
    {
        var parts = version.Split('.');
        return
        (
            parts.Length >= 1 ? int.Parse( parts[ 0 ] ) : 1,
            parts.Length >= 2 ? int.Parse( parts[ 1 ] ) : 0,
            parts.Length >= 3 ? int.Parse( parts[ 2 ] ) : 0
        );
    }

    public List<FileInfo> GetSourceFiles( string? subdirectoryName )
    {
        string path = string.IsNullOrWhiteSpace(subdirectoryName) 
                        ? ProjectDirectoryInfo.FullName 
                        : Path.Combine( ProjectDirectoryInfo.FullName, subdirectoryName );

        var searchDirectory = new DirectoryInfo( path );
        if(  !searchDirectory.Exists )
            return new List<FileInfo>();

        return searchDirectory.GetFiles("*.cs", SearchOption.AllDirectories)?.ToList() ?? new List<FileInfo>();
    }
    public static BuildProject Create( string path )
    {
        var dir = new DirectoryInfo( path );
        if ( !dir.Exists )
            throw new ArgumentException( $"Invalid directory path {path}" );

        var csproj = dir.EnumerateFiles( "*.csproj" , SearchOption.AllDirectories ).FirstOrDefault();
        if ( csproj is null )
            throw new ArgumentException( $"Could not find project file in directory {path}" );

        return new BuildProject( dir , csproj )
        {
            SolutionFileInfo = dir.EnumerateFiles( "*.sln" , SearchOption.AllDirectories ).FirstOrDefault() ,
        };
    }
}