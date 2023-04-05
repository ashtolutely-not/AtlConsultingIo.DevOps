using Microsoft.Data.SqlClient;
using System.Text;
using CliWrap;
using Newtonsoft.Json;
using CliWrap.Buffered;

namespace AtlConsultingIo.Generators;
internal static class SqlEntityGenerator
{
    static readonly Workspace _ws = new AdhocWorkspace();
    public static async Task Run( string configurationFilePath, bool writeToFile = false )
    {
        if ( !File.Exists( configurationFilePath ) )
            return;

        string jsonTxt = File.ReadAllText( configurationFilePath );
        if ( string.IsNullOrEmpty( jsonTxt ) )
            return;

        EFScaffoldConfiguration configuration = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( jsonTxt );
        await Run( configuration, writeToFile );
    }
    public static async Task Run( EFScaffoldConfiguration configuration , bool writeToFile = false )
    {
       if( string.IsNullOrEmpty( configuration.ConnectionString ) )
            throw new ArgumentNullException( nameof( configuration.ConnectionString ));

        var args =  new EFCoreCliCommand
                        .DbContextScaffoldCommand( configuration )
                        .WithTableNames( await GetTableList( configuration ) )
                        .Build();

        var cmd = Cli.Wrap( FileLocations.DotNetEFExecutable )
                    .WithWorkingDirectory( DirectoryLocations.Projects.This )
                    .WithArguments( args );

        if ( writeToFile )
            WriteCommandToFile( string.Concat( EFCoreCliCommand.Alias , TextUtils.WhitespaceChar , args ) );

        await TryExecute( cmd );

    }
    public static void AddSqlInterfaceSyntax( DirectoryInfo entitiesDirectory )
    {
        if( !entitiesDirectory.Exists ) return;
        var files = entitiesDirectory.GetFiles();
        if ( files is not null )
            foreach ( var file in files )
                AddSqlInterfaceSyntax( file );
    }
    public static void AddSqlInterfaceSyntax( FileInfo entityFile )
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText( File.ReadAllText( entityFile.FullName ));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var usings = root.Usings;
        var ns = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if( ns is not null )
        {
           ns = ns.RemoveNodes(ns.DescendantNodes().OfType<ClassDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia);
        }
        ClassDeclarationSyntax? cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if ( cls is null )
            return;

        cls = cls.AddBaseListTypes( SimpleBaseType( ParseName( SourceMetadata.SqlInterfaceIdentifier ) ) );
        cls = (ClassDeclarationSyntax) Formatter.Format( cls , _ws );

        var file = InitializeFileBuilder( usings, ns );
        file.Append( cls.ToString() );

        File.WriteAllText( entityFile.FullName , file.ToString() );

    }

    #region Helpers

    static void WriteCommandToFile( string commandText )
    {
        if ( !Directory.Exists( DirectoryLocations.LocalOutputs.GeneratedSqlEntitiesTest ) )
            Directory.CreateDirectory( DirectoryLocations.LocalOutputs.GeneratedSqlEntitiesTest );

        File.WriteAllText( Path.Combine( DirectoryLocations.LocalOutputs.GeneratedSqlEntitiesTest , "EFCommand".UniqueFileName() + ".txt" ) , commandText );
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

        sb.AppendLine( $"using { SourceMetadata.SqlInterfaceNamespace };" );
        foreach ( var u in usings )
            sb.AppendLine( u.ToString() );

        if ( @namespace is not null )
            sb.AppendLine( @namespace.ToString() );

        sb.AppendLine();

        return sb;
    }
    static async Task<List<string>> GetTableList( EFScaffoldConfiguration configuration )
    {
        var tableResult = await GetSchemaTables( configuration.ConnectionString, configuration.Schema, configuration.IncludeTableViews );
        var tableNames = tableResult.ToList();

        if ( configuration.ExcludedTables.Any() )
            tableNames.RemoveAll( name => configuration.ExcludedTables.Any( tbl => tbl.Equals( name , StringComparison.OrdinalIgnoreCase ) ) );

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
