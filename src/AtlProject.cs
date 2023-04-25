using System.Xml;

namespace AtlConsultingIo.DevOps;

public record AtlProject
{
    private const string _msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
    private const string _commitMessageFile = ".commit-msg";

    private const string _msNamespaceKey = "ms";

    public DirectoryInfo PhysicalDirectory { get; init; } 
    public FileInfo ProjectFile { get; init; } 
    public FileInfo? SolutionFile { get; init; }
    public XmlDocument ProjectXml { get; set; } = new();

    private AtlProject(DirectoryInfo projectDirectory, FileInfo projectFile)
    {
        PhysicalDirectory= projectDirectory;
        ProjectFile= projectFile;

        ProjectXml = new();
        ProjectXml.Load( ProjectFile.FullName );
    }

    public XmlNamespaceManager NamespaceManager()
    {
        var mgr = new XmlNamespaceManager( ProjectXml.NameTable );
        mgr.AddNamespace( _msNamespaceKey, _msbuild );
        return mgr;
    }

    public XmlNode? ProjectNode()
    {
        if( ProjectXml is null ) return null;
        var children = ProjectXml.ChildNodes;

        if( children.Count.Equals( 0 ) ) return null;

        return children[0] is XmlNode node && node.Name.Equals("Project") ? node : null;
    }

    public FileInfo? GetCommitMessageFile()
        => PhysicalDirectory.EnumerateFiles( _commitMessageFile , SearchOption.AllDirectories ).FirstOrDefault();

    public static AtlProject Create( string path )
    {
        var dir = new DirectoryInfo( path );
        if( !dir.Exists )
            throw new ArgumentException($"Invalid directory path {path}");

        var csproj = dir.EnumerateFiles( "*.csproj" , SearchOption.AllDirectories ).FirstOrDefault();
        if( csproj is null )
            throw new ArgumentException($"Could not find project file in directory {path}");

        return new AtlProject( dir, csproj)
        {
            SolutionFile = dir.EnumerateFiles( "*.sln" , SearchOption.AllDirectories ).FirstOrDefault(),
        };
    }


}