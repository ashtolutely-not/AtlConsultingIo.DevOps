using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AtlConsultingIo.DevOps;
internal static class ProjectManager
{
    private static readonly DirectoryInfo _userDirectory = new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ));

    private static readonly EnumerationOptions _recursionOptions = new EnumerationOptions
    {
        RecurseSubdirectories = true,
        MaxRecursionDepth = 10,
        ReturnSpecialDirectories = false
    };


    public static ProjectDirectory? GetProjectDirectory( string projectFolder )
    {
        var directory =  _userDirectory.EnumerateDirectories( projectFolder, _recursionOptions ).FirstOrDefault();
        return directory is not null ? new ProjectDirectory( directory ) : null;
    }


}



public record ProjectDirectory
{
    private const string _msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
    private const string _commitMessageFile = ".commit-msg";

    public const string NamespaceKey = "ms";
    private DirectoryInfo _directory;

    public ProjectDirectory( DirectoryInfo physicalDirectory )
    {
        _directory = physicalDirectory; 
        Path = _directory.FullName;

        ProjectFileInfo = _directory.EnumerateFiles( "*.csproj" , SearchOption.AllDirectories ).FirstOrDefault();
        SolutionFileInfo = _directory.EnumerateFiles( "*.sln" , SearchOption.AllDirectories ).FirstOrDefault();

       if( ProjectFileInfo is null ) return;

        ProjectXml =  new XmlDocument();
        ProjectXml.Load( ProjectFileInfo.FullName );

        ProjectNamespaceManager = new XmlNamespaceManager( ProjectXml.NameTable );
        ProjectNamespaceManager.AddNamespace( NamespaceKey, _msbuild);
    }

    public string Path { get; init; }
    public FileInfo? ProjectFileInfo { get; init; }
    public FileInfo? SolutionFileInfo { get; init; }

    public XmlDocument? ProjectXml { get; set; }
    public XmlNamespaceManager? ProjectNamespaceManager { get; set; }

    public XmlNode? GetProjectXml()
    {
        if( ProjectXml is null ) return null;
        var children = ProjectXml.ChildNodes;

        if( children.Count.Equals( 0 ) ) return null;

        return children[0] is XmlNode node && node.Name.Equals("Project") ? node : null;
    }

    public FileInfo? GetCommitMessageFile()
        => _directory.EnumerateFiles( _commitMessageFile , SearchOption.AllDirectories ).FirstOrDefault();
}