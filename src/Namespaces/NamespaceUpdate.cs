using AtlConsultingIo.DevOps;

using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System.Reflection;
using System.Text;

namespace AtlConsultingIo.NamespaceAnalyzer;
internal static class NamespaceUpdate
{
    //INCOMPLETE
    public static async Task Run( string configFileLocation )
    {
        AnalyzerErrors.ThrowIf.ConfigFileNotFound( configFileLocation );

        var fileText = File.ReadAllText( configFileLocation );
        AnalyzerErrors.ThrowIf.InvalidJsonFile( fileText );

        ProjectNamespaceOptions options = JsonConvert.DeserializeObject<ProjectNamespaceOptions>( fileText );
        AnalyzerErrors.ThrowIf.InvalidConfiguration( options );

        var (ws, comp, namespaces) = await InitializeWorkItems( options );




    }
    public static async Task Inspect( string directoryLocation , string assemblyName )
    {
        string outFile = Path.Combine( CommandParams.TestDirectoryPaths.TestDirectory, $"{assemblyName}.Namespaces_{ Utils.DateTimeTag }.txt");

        var sourceDir = new DirectoryInfo( directoryLocation );
        var srcFiles = sourceDir.GetFiles("*.cs", SearchOption.AllDirectories);

        ProjectId id = ProjectId.CreateNewId();
        VersionStamp version = VersionStamp.Create();

        AdhocWorkspace ws = new AdhocWorkspace();
        Project project = ws.AddProject(
        ProjectInfo
            .Create( id, version, assemblyName, assemblyName, LanguageNames.CSharp)
            .WithDefaultNamespace( assemblyName)
        );

        Compilation comp = CSharpCompilation.Create( assemblyName );

        foreach ( var file in srcFiles )
        {
            SourceText src = SourceText.From( File.ReadAllText( file.FullName ));
            TextLoader loader = TextLoader.From( TextAndVersion.Create( src, VersionStamp.Create(), file.FullName ));
            DocumentInfo info = DocumentInfo
                            .Create( DocumentId.CreateNewId( project.Id ), file.Name, loader: loader )
                            .WithFilePath( file.FullName);

            Document doc = ws.AddDocument( info );
            if ( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
                comp = comp.AddSyntaxTrees( _tree );
        }


        foreach ( var _proj in ws.CurrentSolution.Projects )
        {
            var names = await GetNamespaces( _proj, comp );
            var fileTxt = new StringBuilder();
            foreach ( var n in names )
                fileTxt.AppendLine( n );

            File.WriteAllText( outFile , fileTxt.ToString() );
        }
    }
    public static void ResetToRoot( string sourceDirectory , string assemblyName )
    {
        var projectDir = new DirectoryInfo( sourceDirectory );
        if ( !projectDir.Exists )
            return;

        var srcFiles = projectDir.GetFiles("*.cs", SearchOption.AllDirectories);
        if( !srcFiles.Any() ) return;

        var backupDir = new DirectoryInfo( Path.Combine( CommandParams.TestDirectoryPaths.BackupDirectory, assemblyName.WithDateTagSuffix() ));
        if( !backupDir.Exists )
            backupDir.Create();
        
        var ws = new AdhocWorkspace();

        foreach ( var file in srcFiles )
        {
            string backup = Path.Combine( backupDir.FullName, file.Name );
            file.CopyTo( backup );

            SyntaxTree tree = CSharpSyntaxTree.ParseText( File.ReadAllText( file.FullName ) );
            SyntaxNode root = tree.GetRoot();

            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();

            var clean = InitializeCleanFile( usings, assemblyName );
            
            var namedTypes = NamedTypeNodes(root);
            foreach( var node in namedTypes )
            {
                if( node.HasNamedTypeParent() )
                    continue;

                var srcText = Formatter.Format( node, ws ).ToFullString();
                clean.Append( srcText );
            }

            File.WriteAllText( file.FullName, clean.ToString() );
        }

    }
    static StringBuilder InitializeCleanFile( IEnumerable<UsingDirectiveSyntax> existingUsings, string assemblyName )
    {
        var builder = new StringBuilder();
        foreach ( var u in existingUsings )
        {
            var name = u.Name.ToString();

            if ( !name.StartsWith( "Atl" ) && !name.StartsWith( "TotalLife" ) )
                builder.AppendLine( u.ToString() );
        }

        builder.AppendLine();
        builder.AppendLine( $"namespace { assemblyName };" );

        return builder;
    }
    public static async Task<(AdhocWorkspace Workspace, Compilation Compilation, List<ProjectNamespace> Namespaces)> InitializeWorkItems( ProjectNamespaceOptions options )
    {
        ProjectId id = ProjectId.CreateNewId();
        VersionStamp version = VersionStamp.Create();

        AdhocWorkspace ws = new AdhocWorkspace();
        Project project = ws.AddProject(
            ProjectInfo
                .Create( id, version, options.TargetAssemblyName, options.TargetAssemblyName, LanguageNames.CSharp)
                .WithDefaultNamespace( options.TargetAssemblyName)
        );

        Compilation comp = CSharpCompilation.Create( options.TargetAssemblyName );

        DirectoryInfo projectDir = new DirectoryInfo( options.TargetSourceLocation );
        var srcDirectories = projectDir
                                .GetDirectories("*", SearchOption.AllDirectories)
                                .Where( d => d.ContainsSourceFiles() );

        var namespaces = new List<ProjectNamespace>();

        if ( projectDir.ContainsSourceFiles() && options.DefaultConfig() is DirectoryNamespaceOption _projectDirOption )
            await InitializeOption( _projectDirOption );

        foreach ( var dir in srcDirectories )
            if ( dir.NamespaceOption( options ) is DirectoryNamespaceOption _subDirOption )
                await InitializeOption( _subDirOption );

        async Task InitializeOption( DirectoryNamespaceOption option )
        {
            if ( namespaces is null || comp is null )
                return;

            var exists = namespaces.Any( n => n.Name.Equals( option.NamespaceName, StringComparison.OrdinalIgnoreCase));
            var ns = namespaces
                    .FirstOrDefault(
                        n => n.Name.Equals( option.NamespaceName, StringComparison.OrdinalIgnoreCase)
                     ) ?? new( option.NamespaceName );

            ns.DirectoryOptions.Add( option );

            var srcFiles = projectDir.CSharpFiles();
            foreach ( var file in srcFiles )
            {
                var docInfo = GetDocumentInfo( file, ws.CurrentSolution.ProjectIds[0] );

                var doc = ws.AddDocument( docInfo );
                AnalyzerErrors.ThrowIf.InvalidDocument( doc );

                if ( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
                    comp = comp.AddSyntaxTrees( _tree );
            }

            if ( !exists )
                namespaces.Add( ns );
        }

        return (ws, comp, namespaces);
    }
    public static async Task<List<NamespaceTracker>> InitializeTrackers( AdhocWorkspace workspace , CSharpCompilation compilation )
    {
        var trackers = new List<NamespaceTracker>();
        var project = workspace.CurrentSolution.Projects.ElementAt( 0 );

        foreach ( var doc in project.Documents )
        {
            if ( await doc.GetSyntaxRootAsync() is not SyntaxNode _root || await doc.GetSyntaxTreeAsync() is not SyntaxTree _tree )
                continue;

            var model = compilation.GetSemanticModel( _tree );
            if ( model is null )
                throw new InvalidOperationException( $"No semantics returned for {doc.FilePath ?? doc.Name}.  Cannot update namespaces." );

            var tracker = new NamespaceTracker( doc, model );
            var ns = _root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if ( ns is not null )
                tracker = tracker.WithNamespaceSyntax( ns );

            var usings = _root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            if ( usings is not null && usings.Any() )
                tracker.UsingSyntax.AddRange( usings );

            trackers.Add( tracker );
        }

        SetDependencies( trackers );
        return trackers;
    }
    static DocumentInfo GetDocumentInfo( FileInfo sourceFile , ProjectId projectId )
    {
        SourceText txt = SourceText.From( File.ReadAllText( sourceFile.FullName ) );
        TextLoader loader = TextLoader.From( TextAndVersion.Create( txt, VersionStamp.Create(), sourceFile.FullName ));
        DocumentInfo info = DocumentInfo
                                .Create( DocumentId.CreateNewId( projectId ), sourceFile.Name, loader: loader )
                                .WithFilePath( sourceFile.FullName);

        return info;
    }
    static void SetDependencies( List<NamespaceTracker> trackers )
    {
        for ( int i = 0; i < trackers.Count; i++ )
        {
            var _toUpdate = trackers[i];

            if ( string.IsNullOrEmpty( _toUpdate.NamespaceName() ) )
                continue;

            foreach ( var tracker in trackers )
            {
                if ( tracker.Equals( _toUpdate ) ) continue;
                if ( tracker.UsingNamespaces().Contains( _toUpdate.NamespaceName() ) )
                    _toUpdate.Dependencies.Add( tracker.Document );
            }
        }
    }
    static async Task<IEnumerable<SyntaxNode>> GetTypeDeclarations( Document document )
    {
        if ( await document.GetSyntaxRootAsync() is not SyntaxNode _root )
            return Enumerable.Empty<SyntaxNode>();

        return NamedTypeNodes(_root);
    }
    static IEnumerable<SyntaxNode> NamedTypeNodes( SyntaxNode root )
    {
        IEnumerable<SyntaxNode> classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        IEnumerable<SyntaxNode> records = root.DescendantNodes().OfType<RecordDeclarationSyntax>();
        IEnumerable<SyntaxNode> structs = root.DescendantNodes().OfType<StructDeclarationSyntax>();
        IEnumerable<SyntaxNode> interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
        IEnumerable<SyntaxNode> enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
        return classes.Concat( records.Concat( structs.Concat(interfaces.Concat(enums)) ) );
    }
    static async Task<List<string>> GetNamespaces( Project proj , Compilation comp )
    {
        var names = new List<string>();
        foreach ( var _doc in proj.Documents )
        {
            var tree = await _doc.GetSyntaxTreeAsync();
            var root = await _doc.GetSyntaxRootAsync();

            if ( tree is null || root is null ) continue;

            var model = comp.GetSemanticModel( tree );
            var ns = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

            if ( ns is null )
            {
                Console.WriteLine( $"NO NAMESPACE: {_doc.Name}" );
                continue;
            }

            var nameSyntax = ns.DescendantNodes().OfType<QualifiedNameSyntax>().FirstOrDefault();

            var nameString = nameSyntax?.ToString();

            if ( !string.IsNullOrEmpty( nameString ) )
                names.Add( nameString );
        }

        return names;
    }

    static bool HasNamedTypeParent( this SyntaxNode childNode )
    {
        if( childNode.Parent is not SyntaxNode _parent)
            return false;

        return _parent is ClassDeclarationSyntax || _parent is RecordDeclarationSyntax || _parent is StructDeclarationSyntax;
    }

}
