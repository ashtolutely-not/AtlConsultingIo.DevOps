namespace AtlConsultingIo.NamespaceAnalyzer;

internal record struct NamespaceTracker( Document Document , SemanticModel Model )
{
    public NamespaceDeclarationSyntax? NamespaceSyntax { get; init; } = default;
    public List<Document> Dependencies { get; init; } = new List<Document>();
    public List<UsingDirectiveSyntax> UsingSyntax { get; init; } = new List<UsingDirectiveSyntax>();
    public NamespaceTracker WithNamespaceSyntax( NamespaceDeclarationSyntax namespaceSyntax )
        => this with { NamespaceSyntax = namespaceSyntax };
    public NamespaceTracker WithSemantics( SemanticModel semanticModel )
        => this with { Model = semanticModel };
}