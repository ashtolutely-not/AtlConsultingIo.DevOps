using Newtonsoft.Json;
namespace AtlConsultingIo.DevOps;


internal struct ProjectNamespaceOptions
{
    public string TargetSourceLocation { get; init; }
    public string TargetAssemblyName { get; init; }
    public string[] IgnoreDirectories { get; init; }
    public List<DirectoryNamespaceOption> DirectoryOptions { get; set;}
    public ProjectNamespaceOptions( string settingsLocation )
    {
        this = JsonConvert.DeserializeObject<ProjectNamespaceOptions>( File.ReadAllText( settingsLocation ) );
    }
    public bool IsEmpty => Equals( Default );
    public static readonly ProjectNamespaceOptions Default = new ProjectNamespaceOptions();
}