namespace AtlConsultingIo.NamespaceAnalyzer;
internal record ProjectNamespace( string Name )
{
    public List<Document> Documents { get; init; } = new List<Document>();
    public List<DirectoryNamespaceOption> DirectoryOptions { get; init; } = new List<DirectoryNamespaceOption>();
    public async Task<IEnumerable<string>> GetCurrentNamespaces( Compilation compilation )
    {
        var list = new List<string>();
        foreach( Document doc in Documents )
        {
            if( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
                if( await doc.GetSyntaxRootAsync() is SyntaxNode _root )
                    if( _root.DescendantNodes().FirstOrDefault( n => n is NamespaceDeclarationSyntax) is NamespaceDeclarationSyntax _namespace )
                        if( compilation.GetSemanticModel( _tree ).GetDeclaredSymbol( _namespace ) is INamespaceSymbol _symbol )
                            list.Add( _symbol.Name );
                    
        }


        return list;
    }
}

