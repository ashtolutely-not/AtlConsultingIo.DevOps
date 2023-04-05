namespace AtlConsultingIo.NamespaceAnalyzer;


internal static class AnalyzerExtensions
{
    public static DirectoryNamespaceOption? DefaultConfig( this ProjectNamespaceOptions options )
        => options.DirectoryOptions
            .FirstOrDefault( c => 
                c.DirectoryName.Equals("default", StringComparison.OrdinalIgnoreCase) ||
                c.DirectoryName.Equals( options.TargetAssemblyName, StringComparison.OrdinalIgnoreCase) 
            );
    public static async Task<INamespaceSymbol?> NamespaceSymbol ( this Document doc, Compilation compilation )
    {
        if( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
            if( await doc.GetSyntaxRootAsync() is SyntaxNode _root )
                if( _root.DescendantNodes().FirstOrDefault( n => n is FileScopedNamespaceDeclarationSyntax) is FileScopedNamespaceDeclarationSyntax _namespace )
                    if( compilation.GetSemanticModel( _tree ).GetDeclaredSymbol( _namespace ) is INamespaceSymbol _symbol )    
                        return _symbol;

        return null;
    }

    public static IEnumerable<FileInfo> CSharpFiles( this DirectoryInfo directory )
        => directory.GetFiles( "*.cs" , SearchOption.TopDirectoryOnly );
    public static bool Ignore( this DirectoryInfo directory , ProjectNamespaceOptions options )
        => options.IgnoreDirectories.Any( d => d.Equals( directory.Name , StringComparison.OrdinalIgnoreCase ) );
    public static bool ContainsSourceFiles( this DirectoryInfo directory )
        => directory.GetFiles( "*.cs" , SearchOption.TopDirectoryOnly ).Any();
    public static string NamespaceName( this NamespaceTracker tracker )
        => tracker.NamespaceSyntax?.Name.Span.ToString() ?? String.Empty;
    public static IEnumerable<string> UsingNamespaces( this NamespaceTracker tracker )
        => tracker.UsingSyntax.Select( u => u.Name.Span.ToString() );

    public static DirectoryNamespaceOption? NamespaceOption( this DirectoryInfo sourceDirectory, ProjectNamespaceOptions projectOptions )
        => projectOptions.DirectoryOptions.FirstOrDefault( o => o.DirectoryName.Equals( sourceDirectory.Name, StringComparison.OrdinalIgnoreCase));
    public static async Task<IEnumerable<string>> UsingNamespaces( this Document doc )
    {
        if ( await doc.GetSyntaxRootAsync() is not SyntaxNode _root || await doc.GetSyntaxTreeAsync() is not SyntaxTree _tree )
            return Enumerable.Empty<string>();

        var usings =  _root.DescendantNodes().OfType<UsingDirectiveSyntax>();
        if( !usings.Any() )
            return Enumerable.Empty<string>();

        var list = new List<string>();  
        foreach( var u in usings )
        {
            var name = u.DescendantNodes().OfType<NameSyntax>().FirstOrDefault();
            if( name is not null )
                list.Add( name.ToString() );
        }

        return list;
    }

}
