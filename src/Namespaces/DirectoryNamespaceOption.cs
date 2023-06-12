namespace AtlConsultingIo.DevOps;
internal record DirectoryNamespaceOption
{
    public string DirectoryName { get; init; } = String.Empty;
    public string NamespaceName { get; init; } = String.Empty;

    public DirectoryInfo? DirectoryInfo { get; init; }  

    //Out of scope for now
    //public Dictionary<string,string> FileOverrides { get; init; } = new Dictionary<string,string>();

    public static readonly DirectoryNamespaceOption Default = new DirectoryNamespaceOption();
}
