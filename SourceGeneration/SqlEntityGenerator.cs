using Microsoft.Data.SqlClient;
using System.Text;
using CliWrap;
using Newtonsoft.Json;
using CliWrap.Buffered;
using AtlConsultingIo.DevOps;

namespace AtlConsultingIo.Generators;
internal static class SqlEntityGenerator
{
    static readonly Workspace _ws = new AdhocWorkspace();
    public static async Task Run( string configurationFilePath , bool writeToFile = false )
    {
        if ( !File.Exists( configurationFilePath ) )
            return;

        string jsonTxt = File.ReadAllText( configurationFilePath );
        if ( string.IsNullOrEmpty( jsonTxt ) )
            return;

        EFScaffoldConfiguration configuration = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( jsonTxt );
        await Run( configuration , writeToFile );
    }
    public static async Task Run( EFScaffoldConfiguration configuration , bool writeToFile = false )
    {
        if ( string.IsNullOrEmpty( configuration.ConnectionString ) )
            throw new ArgumentNullException( nameof( configuration.ConnectionString ) );

        var tables = await GetTableList( configuration );

        var args =  new EFCoreCliCommand
                        .DbContextScaffoldCommand( configuration )
                        .WithTableNames( tables )
                        .Build();

        var cmd = Cli.Wrap( CommandParams.FilePaths.DotNetEFExecutable )
                    .WithWorkingDirectory( CommandParams.AtlConsultingIoProjects.DirectoryPaths.DevOps )
                    .WithArguments( args );

        if ( writeToFile )
            WriteCommandToFile( string.Concat( EFCoreCliCommand.Alias , Extensions.WhitespaceChar , args ) );

        await TryExecute( cmd );
    }

    public static void AdjustNames( EFScaffoldConfiguration configuration )
    {
        if( !configuration.EntityNameAdjustments.Any() ) return;

        var entitiesDir = new DirectoryInfo( configuration.EntitiesOutDirectory );
        var ctxFile = new FileInfo(Path.Combine( configuration.ContextOutDirectory, configuration.ContextName + ".cs"));

        if( !entitiesDir.Exists || !ctxFile.Exists ) return;

        foreach( var kv in configuration.EntityNameAdjustments )
        {
            var oldFile = new FileInfo( Path.Combine( entitiesDir.FullName, kv.Key + ".cs"));
            if( !oldFile.Exists ) continue;

            var oldFileText = File.ReadAllText(oldFile.FullName);
            var newFileText = oldFileText.Replace(kv.Key,kv.Value);

            var newFile = new FileInfo( Path.Combine(entitiesDir.FullName, kv.Value + ".cs"));
            File.WriteAllText( newFile.FullName, newFileText);
            File.Delete( oldFile.FullName );

            var contextFileText = File.ReadAllText(ctxFile.FullName);
            contextFileText = contextFileText.Replace( $"DbSet<{kv.Key}>", $"DbSet<{kv.Value}>" );
            File.WriteAllText( ctxFile.FullName, contextFileText );
        }
    }

    public static void AddSqlInterfaceSyntax( FileInfo entityFile )
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText( File.ReadAllText( entityFile.FullName ));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        ClassDeclarationSyntax? cls = root.DescendantNodes()
                                        .OfType<ClassDeclarationSyntax>()
                                        .FirstOrDefault();
        if( cls is null ) return;

        FileScopedNamespaceDeclarationSyntax? nsNode 
           = root.DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if( nsNode is not null )
        {
            IEnumerable<ClassDeclarationSyntax> clsNodes = nsNode.DescendantNodes().OfType<ClassDeclarationSyntax>();
            if( clsNodes.Any() )
                nsNode = nsNode.RemoveNodes( clsNodes, SyntaxRemoveOptions.KeepNoTrivia );
        }    
        
        var file = InitializeFileBuilder( 
                root.Usings, 
                nsNode
            );

        file.Append( ((ClassDeclarationSyntax) Formatter.Format( cls , _ws )).ToString() );

        File.WriteAllText( entityFile.FullName , file.ToString() );

    }

    #region Helpers

    static void WriteCommandToFile( string commandText )
    {
        if ( !Directory.Exists( CommandParams.TestDirectoryPaths.ExigoEntitiesTests ) )
            Directory.CreateDirectory( CommandParams.TestDirectoryPaths.ExigoEntitiesTests );

        File.WriteAllText( Path.Combine( CommandParams.TestDirectoryPaths.ExigoEntitiesTests , "EFCommand".UniqueFileName() + ".txt" ) , commandText );
    }
    static async Task TryExecute( Command command )
    {
        try
        {
            var _cmd = command.WithValidation( CommandResultValidation.None );
            var result = await _cmd.ExecuteBufferedAsync();
            Console.WriteLine( result.StandardOutput );
        }
        catch ( Exception e )
        {
            Console.WriteLine( "RESULT: CLI Command FAILED. " );
            Console.WriteLine( $"EXCEPTION MESSAGE : {e.Message}" );
            Console.WriteLine( $"INNER EXCEPTION MESSAGE : {e.InnerException?.Message ?? "NULL"}" );
        }
    }
    static StringBuilder InitializeFileBuilder( SyntaxList<UsingDirectiveSyntax> usings , FileScopedNamespaceDeclarationSyntax? @namespace )
    {
        var sb = new StringBuilder();
        sb.AppendLine( $"// Generated from EF Core Tools " );
        sb.AppendLine( "// Manual modification of files not recommended, could result in errors when using SqlService." );

        sb.AppendLine();
        sb.AppendLine();

        sb.AppendLine( $"using {CommandParams.AtlConsultingIoProjects.ExigoGeneratorNamespaces.DatabaseEntities};" );
        foreach ( var u in usings )
            sb.AppendLine( u.ToString() );

        if ( @namespace is not null )
        {
            sb.AppendLine();
            sb.AppendLine( @namespace.ToString() );
        }

        sb.AppendLine();

        return sb;
    }
    static async Task<List<string>> GetTableList( EFScaffoldConfiguration configuration )
    {
        var tableResult = await GetSchemaTables( configuration.ConnectionString, configuration.Schema, configuration.IncludeTableViews );
        var tableNames = tableResult.ToList();

        foreach( string table in configuration.ExcludedTables )
            tableNames.RemoveAll( x => x.Equals( table, StringComparison.OrdinalIgnoreCase));

        return tableNames;
    }
    static async Task<IEnumerable<string>> GetSchemaTables( string connectionString , string schema , bool includeViews )
    {
        var cn = new SqlConnection( connectionString );
        await cn.OpenAsync();

        var query = $@"select distinct t.TABLE_NAME
                        from INFORMATION_SCHEMA.TABLES t
                        inner join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        on tc.TABLE_NAME = t.TABLE_NAME
                        inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE u
                        on u.TABLE_NAME = t.TABLE_NAME 
                        and tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                        AND t.TABLE_SCHEMA = '{ schema }'";

        if ( !includeViews )
            query += @" AND t.TABLE_TYPE = 'BASE TABLE'";

        var cmd = new SqlCommand( query, cn );
        var rdr = await cmd.ExecuteReaderAsync();

        var tables = new List<string>();
        while ( rdr.Read() )
            tables.Add( rdr.GetString( 0 ) );

        return tables;
    }

    #endregion


}
