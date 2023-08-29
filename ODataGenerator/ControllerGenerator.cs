using AtlConsultingIo.Generators;

using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.EntityFrameworkCore;
using static Dapper.SqlMapper;
using Microsoft.CodeAnalysis;
using System.ComponentModel;

namespace AtlConsultingIo.DevOps.ODataGenerator;
internal static class ControllerGenerator
{
    private const string Whitespace = " ";
    private const string CSharpExtension = ".cs";

    private const string ProviderNamespace = "Microsoft.EntityFrameworkCore.SqlServer";
    public const string EfCommandAlias = "dotnet ef";
    public const string EfCliPath = @"C:\Users\ashto\.dotnet\tools\dotnet-ef.exe";

    private static readonly List<SqlTableMetadata> _emptyResult = new();
    public static async Task GenerateEntitiesAndControllers( ODataGeneratorSettings settings )
    {
        var entityMetadata = await GenerateEntities( settings );
        entityMetadata = await UpdateTableMetadata( settings.ConnectionString, entityMetadata );

        List<ODataControllerMetadata> controllerMeta = await BuildControllerMetadata( settings, entityMetadata );
    }

    private static async Task<List<ODataControllerMetadata>> BuildControllerMetadata( ODataGeneratorSettings settings , List<EntitySourceMetadata> entityMetadata )
    {
        List<ODataControllerMetadata> controllerMetadata
            = await InitializeControllers(
                settings.ConnectionString,
                entityMetadata,
                new DbContextCommandArgs(settings));

        for ( int i = 0 ; i < controllerMetadata.Count ; i++ )
            controllerMetadata[ i ] = AddRelatedEntities ( controllerMetadata[ i ] , entityMetadata );

        return controllerMetadata;
    }
    private static async Task<List<EntitySourceMetadata>> UpdateTableMetadata(string connectionString, List<EntitySourceMetadata> metadata )
    {
        for ( int i = 0 ; i < metadata.Count ; i++ )
        {
            var itemRef = metadata[i];
            var updatedTableRef = await FillColumnMetadata( connectionString, itemRef );
            itemRef = itemRef with { SourceTableMeta = updatedTableRef };
        }    
        return metadata;
    }
    public static async Task<List<EntitySourceMetadata>> GenerateEntities( ODataGeneratorSettings settings )
    {
        var tables = await GetControllerTables( settings );

        DbContextCommandArgs contextArgs = new( settings );
        DbEntitiesCommandArgs entityArgs = new( settings );
        await GenerateEntities ( settings , contextArgs , entityArgs , tables );

        List<EntitySourceMetadata> entities = new();
        for ( int i = 0 ; i < tables.Count ; i++ )
            entities.Add ( FindGeneratedEntityFile ( entityArgs , tables[ i ] ) );

        if ( !contextArgs.FileInfo.Exists )
            throw ContextFileNotFound ( contextArgs );

        return SetNamingConventions ( contextArgs , entities );
    }


    public static async Task<List<SqlTableMetadata>> GetControllerTables( ODataGeneratorSettings settings )
    {
        SqlConnection connection = new SqlConnection( settings.ConnectionString );
        var eligibleTables = await GetTableCandidates( connection );
        if ( !eligibleTables.Any ( ) )
            throw new Exception ( $"No tables with primary key definitions found in database {connection.Database}, cannot generate entity types." );

        eligibleTables = FilterCandidates( settings, eligibleTables );
        if ( !eligibleTables.Any ( ) )
            throw new Exception ( $"No tables with primary key definitions found in database {connection.Database}, cannot generate entity types." );

        return eligibleTables;
    }
    private static async Task<List<SqlTableMetadata>> GetTableCandidates( SqlConnection initializedConnection )
    {
        await initializedConnection.OpenAsync ( );

        string databaseName = initializedConnection.Database;
        var tableNames = await initializedConnection.QueryAsync<SqlTableMetadata>( SelectTablesWithPrimaryKeyDefinition, new { databaseName } );

        return tableNames?.ToList ( ) ?? _emptyResult;
    }
    private static List<SqlTableMetadata> FilterCandidates( ODataGeneratorSettings setting , List<SqlTableMetadata> candidates )
    {
        var _candidates = candidates;

        if ( !string.IsNullOrWhiteSpace ( setting.SchemaConstraint ) )
        {
            Console.WriteLine ( $"Generator settings includes database schema constraint, removing eligible tables not in the {setting.SchemaConstraint} schema." );
            _ = _candidates.RemoveAll ( i => !i.SchemaName.CaseInsensitiveEquals ( setting.SchemaConstraint ) );
            Console.WriteLine ( $"{_candidates.Count} tables remaining after schema constraint applied." );
        }

        if ( setting.TableFilters is not null 
            && setting.TableFilters.Count > 0 )
        {
            Console.WriteLine ( $"Generator settings includes table filter values, removing eligible tables not included in filters list." );
            _ = _candidates.RemoveAll ( i => setting.TableFilters.Contains ( i.Name , StringComparer.OrdinalIgnoreCase ) );
            Console.WriteLine ( $"{_candidates.Count} tables remaining after table filters applied." );
        }

        return _candidates;
    }

    private static async Task GenerateEntities( ODataGeneratorSettings settings, DbContextCommandArgs contextArgs, DbEntitiesCommandArgs entityArgs, List<SqlTableMetadata> entityTables )
    {
        var cliCmd 
            = Cli.Wrap ( EfCliPath )
                .WithWorkingDirectory ( CommandParams.AtlConsultingIoProjects.DirectoryPaths.DevOps )
                .WithArguments ( BuildScaffoldCommandArgs ( settings ,contextArgs, entityArgs, entityTables ) );

        await Console.Out.WriteLineAsync ( "Executing EF Core Command" );
        await Console.Out.WriteLineAsync (  "Command Text: \r\n" + cliCmd.ToString() );

        await cliCmd.ExecuteBufferedAsync ( );
    }
    private static string BuildScaffoldCommandArgs( ODataGeneratorSettings settings , DbContextCommandArgs contextArgs , DbEntitiesCommandArgs entityArgs , List<SqlTableMetadata> entityTables )
    {
        StringBuilder args = new StringBuilder( "dbcontext scaffold" );
        args.Append ( Whitespace );

        args.Append( settings.ConnectionString.SurroundWithDoubleQuotes() );
        args.Append( Whitespace );

        args.Append ( ProviderNamespace.SurroundWithDoubleQuotes() );
        args.Append ( Whitespace );

        args.Append ( contextArgs.GetCommandString ( ) );
        args.Append ( Whitespace );

        args.Append ( entityArgs.GetCommandString ( ) );
        args.Append ( Whitespace );

        foreach ( var table in entityTables )
            args.Append ( TableArg ( table ) + Whitespace );

        args.Append ( ScaffoldCommandArgs.UseDataAnnotations + Whitespace );
        args.Append ( ScaffoldCommandArgs.NoOnConfiguringMethod + Whitespace );
        args.Append ( ScaffoldCommandArgs.NoBuildFlag + Whitespace );
        args.Append ( ScaffoldCommandArgs.ForceOverwrite + Whitespace );

        // So the pluralizer reference is actually better at renaming something that is already plural
        // So we'll just use the database names then go pack and change them after the EF files are created
        args.Append ( ScaffoldCommandArgs.NoPluralizer + Whitespace );
        args.Append ( ScaffoldCommandArgs.UseDatabaseNames + Whitespace );

        return args.ToString ( );
    }
    public static string TableArg( SqlTableMetadata tableMeta )
        => string.Join ( Whitespace , ScaffoldCommandArgs.TableName , tableMeta.QualifiedTableName );

    private static EntitySourceMetadata FindGeneratedEntityFile( DbEntitiesCommandArgs entityCommandArgs, SqlTableMetadata sourceTableMeta )
    {
        FileInfo entityFile = new( Path.Combine( entityCommandArgs.OutPath, sourceTableMeta.Name ) + CSharpExtension );
        if( !entityFile.Exists )
            throw EntityFileNotFound( entityCommandArgs.OutPath, sourceTableMeta.Name );

        return new EntitySourceMetadata( sourceTableMeta, entityFile );
    }
    private static async Task<SqlTableMetadata> FillColumnMetadata( string connectionString, EntitySourceMetadata entityMeta )
    {
        var cols = await GetColumnMetadata( connectionString, entityMeta.SourceTableMeta );
        return entityMeta.SourceTableMeta with { ColumnMetadata = cols };
    }
    private static async Task<List<SqlColumnMetadata>> GetColumnMetadata( string connectionString , SqlTableMetadata tableMeta )
    {
        var conn = new SqlConnection( connectionString );
        await conn.OpenAsync ( );

        var cols = await conn.QueryAsync<SqlColumnMetadata>( SqlColumnMetadata.TableSelect( tableMeta ) );
        return cols?.AsList ( ) ?? new ( );
    }

    public static List<EntitySourceMetadata> SetNamingConventions( DbContextCommandArgs contextFileArgs, List<EntitySourceMetadata> tableEntities )
    {
        var entityMetadata = tableEntities;
        for ( int i = 0 ; i < entityMetadata.Count ; i++ )
        {
            EntitySourceMetadata srcMeta = entityMetadata[i];
            srcMeta = srcMeta with { EntityFileMeta = NormalizeTypeName ( contextFileArgs , srcMeta ) };
            srcMeta = PluralizeContextPropertyName ( contextFileArgs , srcMeta );
            AddTableAttributeText ( srcMeta );

        }
        return entityMetadata;
    }
    private static FileInfo NormalizeTypeName( DbContextCommandArgs contextFileMeta, EntitySourceMetadata metadata )
    {
        string singular = metadata.SourceTableMeta.Name.Singularize();
        if( metadata.SourceTableMeta.Name.Equals( singular ) )
            return metadata.EntityFileMeta;

        string oldName = metadata.SourceTableMeta.Name;
        string newName = singular;
        if( oldName.Equals(newName) )
            return metadata.EntityFileMeta;

        UpdateDbSetTypeParam( contextFileMeta, oldName, newName );  

        FileInfo updatedFile = UpdateEntityFile( metadata.EntityFileMeta, oldName, newName );
        File.Delete ( metadata.EntityFileMeta.FullName );

        return updatedFile;
    }
    private static FileInfo UpdateEntityFile( FileInfo currentValue, string oldName, string newName )
    {
        var oldFileText = File.ReadAllText( currentValue.FullName);
        var newFileText = oldFileText.Replace(oldName, newName);

        string newFileName =  newName + CSharpExtension;
        var newLocation = new FileInfo ( Path.Combine ( currentValue.Directory!.FullName! , newFileName ) );

        File.WriteAllText( newLocation.FullName, newFileText );
        return newLocation;
    }
    private static void UpdateDbSetTypeParam( DbContextCommandArgs dbContextFileMeta, string oldTypeName, string newTypeName )
    {
        var contextFileText = File.ReadAllText(dbContextFileMeta.FileInfo.FullName);
        contextFileText = contextFileText.Replace ( $"DbSet<{oldTypeName}>" , $"DbSet<{newTypeName}>" );
        File.WriteAllText ( dbContextFileMeta.FileInfo.FullName , contextFileText );
    }
    private static EntitySourceMetadata PluralizeContextPropertyName( DbContextCommandArgs dbContextArgs , EntitySourceMetadata metadata )
    {
        var dbContextLines = File.ReadAllLines( dbContextArgs.FileInfo.FullName ).ToList();
        SyntaxTree tree = CSharpSyntaxTree.ParseText( string.Join( "\r\n", dbContextLines ) );
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var dbContextClassNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if ( dbContextClassNode is null )
            throw new Exception ( "Cannot locate DbContext class syntax node." );

        var propertyNode
            = dbContextClassNode
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                ?.FirstOrDefault( p => p.Identifier.ToString().Equals( metadata.SourceTableMeta.Name ) );

        if ( propertyNode is null )
            throw new Exception ( $"Cannot locate syntax node {metadata.SourceTableMeta.Name} DbSet property." );

        string pluralName = metadata.SourceTableMeta.Name.Pluralize();
        string  propertyName = propertyNode.Identifier.ToString();
        if ( pluralName.Equals ( propertyName ) )
            return metadata;

        string? lineToUpdate = dbContextLines.FirstOrDefault( l => l.Contains($"{propertyName} {{") );
        if ( lineToUpdate.IsNullOrWhitespace ( ) )
            return metadata;

        int lineIndex = dbContextLines.IndexOf( lineToUpdate! );
        if ( lineIndex < 0 )
            return metadata;

        string targetTxt = propertyName  + " {";
        string replaceTxt = pluralName + " {";
        string updatedLine = lineToUpdate!.Replace( targetTxt, replaceTxt );
        dbContextLines[ lineIndex ] = updatedLine;

        File.WriteAllLines ( dbContextArgs.FileInfo.FullName , dbContextLines );

        return metadata with { DbSetName = pluralName };
    }
    private static void AddTableAttributeText( EntitySourceMetadata sourceMeta )    
    {
        string attrText = AttributeText( sourceMeta.SourceTableMeta );
        var entityFileLines = File.ReadAllLines( sourceMeta.EntityFileMeta.FullName ).ToList();
        SyntaxTree tree = CSharpSyntaxTree.ParseText( string.Join( "\r\n", entityFileLines ) );
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if ( classNode is null )
            throw new Exception ( "Cannot locate DbContext class syntax node." );

        string clsName = classNode.Identifier.ToString();
        var searchLine = entityFileLines.FirstOrDefault( l => l.Contains($"public partial class {clsName}"));
        if( searchLine is null ) return;

        var declarationLineIndex = entityFileLines.IndexOf( searchLine );
        var newLines = new List<string>();

        if( declarationLineIndex == 0 )
        {
            newLines.Add( attrText );
            newLines.AddRange( entityFileLines );
        }
        else
        {
            for ( int i = 0 ; i < declarationLineIndex ; i++ )
                newLines.Add ( entityFileLines[ i ] );

            newLines.Add ( attrText );

            for ( int i = declarationLineIndex ; i < entityFileLines.Count ; i++ )
                newLines.Add ( entityFileLines[ i ] );
        }

        string fileTxt = string.Join("\r\n", newLines);
        File.WriteAllLines( sourceMeta.EntityFileMeta.FullName, newLines );
    }


    public static async Task<List<ODataControllerMetadata>> InitializeControllers( string connectionString , List<EntitySourceMetadata> entityMetadata, DbContextCommandArgs contextFileMeta )
    {
        List<ODataControllerMetadata> controllers = new();
        for ( int i = 0 ; i < entityMetadata.Count ; i++ )
            controllers.Add ( await InitializeControllerMetadata ( connectionString , entityMetadata[ i ] , contextFileMeta ) );
        return controllers;
    }
    private static async Task<ODataControllerMetadata> InitializeControllerMetadata( string connectionString, EntitySourceMetadata entityMetadata, DbContextCommandArgs contextFileArgs )
    {
        ODataControllerMetadata controller = new ODataControllerMetadata( entityMetadata, contextFileArgs );
        List<SqlIndexMetadata> tableIndexes = await GetTableIndexes( connectionString, controller.TableEntityMetadata.SourceTableMeta );

        return AddIndexRoutes( controller, tableIndexes );
    }
    private static ODataControllerMetadata AddRelatedEntities(  ODataControllerMetadata controller , List<EntitySourceMetadata> controllerTables )
    {
        var _controller = controller;
        var _tables = controllerTables;

        _ = _tables.Remove( controller.TableEntityMetadata );

        foreach ( var table in _tables ) 
            if( IsRelatedEntity( _controller.TableEntityMetadata, table ) )
                _controller.RelatedEntities.Add( table );

        return _controller;
    }

    private static bool IsRelatedEntity( EntitySourceMetadata entity, EntitySourceMetadata otherEntity )
    {
        if( entity.SourceTableMeta.ColumnMetadata is null || otherEntity.SourceTableMeta.ColumnMetadata is null )
            return false;

        var primaryKeyCols 
            = entity.SourceTableMeta
                    .ColumnMetadata
                    .Where( col => col.IsPrimaryKey )
                    ?.Select( col => col.Name )
                    ?.ToList();

        if( primaryKeyCols is null )
            return false;

        List<string> otherColumns = otherEntity.SourceTableMeta.ColumnMetadata.Select( col => col.Name ).ToList();
        return primaryKeyCols.Intersect( otherColumns ).Count().Equals( primaryKeyCols.Count );

    }
    private static ODataControllerMetadata AddIndexRoutes( ODataControllerMetadata controller, List<SqlIndexMetadata> indexes )
    {
        var _controller = controller;
        // Route template conventions are different for primary keys since it's guaranteed to return a single row
        for ( int i = 0 ; i < indexes.Count ; i++ ) 
            if( indexes[i].IsPrimary.Equals(false) )
            _controller.IndexQueryRoutes.Add(
                    new IndexQueryRoute
                    {
                        PluralizedTableName = _controller.TableEntityMetadata.SourceTableMeta.Name.Pluralize ( ) ,
                        RouteParams = indexes[i]
                                        .IndexColumns
                                        .Select( c => new IndexRouteParam(c.ColumnDataType, c.ColumnName.ToCamelCase()))
                                        .ToList(),
                    }
                );

        var pkIndex = indexes.FirstOrDefault( index => index.IsPrimary  );
        if( pkIndex != null )
            _controller = _controller with { PrimaryKeyRoute = new( pkIndex ) };

        return _controller;
    }
    private static async Task<List<SqlIndexMetadata>> GetTableIndexes(string connectionString, SqlTableMetadata tableMeta )
    {
        var conn = new SqlConnection( connectionString );
        await conn.OpenAsync();

        var cmdTxt = SqlIndexMetadata.TableSelect( tableMeta );
        var resultMap = new Dictionary<string,SqlIndexMetadata>();

        var result = await conn.QueryAsync<SqlIndexMetadata, SqlIndexColumn, SqlIndexMetadata>( cmdTxt, (index,column ) => 
        {
            SqlIndexMetadata meta = resultMap.TryGetValue( index.Name, out var obj ) && obj is SqlIndexMetadata _index ? _index : index; 
            if( !meta.IndexColumns.Any(c => c.ColumnName.Equals(column.ColumnName)) )
                meta.IndexColumns.Add( column );

            resultMap[ index.Name ] = meta;
            return meta;

        }, splitOn: nameof(SqlIndexColumn.ColumnName));

        return result?.ToList() ?? new List<SqlIndexMetadata>();
    }

    internal struct ScaffoldCommandArgs
    {
        public const string TargetProject =  "--project";
        public const string StartupProject = "--startup-project";

        //Constraints
        public const string SchemaName = "--schema";
        public const string TableName = "--table";

        public const string MSBuildExtension = "--msbuildprojectextensionspath";

        public const string UseDataAnnotations = "--data-annotations";
        public const string UseDatabaseNames = "--use-database-names";
        public const string ForceOverwrite = "--force";
        public const string NoBuildFlag = "--no-build";
        public const string NoOnConfiguringMethod = "--no-onconfiguring";
        public const string NoPluralizer = "--no-pluralize";
    }
    private static async Task TryExecute( Command command )
    {
        try
        {
            var _cmd = command.WithValidation( CommandResultValidation.None );
            var result = await _cmd.ExecuteBufferedAsync();
            Console.WriteLine ( result.StandardOutput );
        }
        catch ( Exception e )
        {
            Console.WriteLine ( "RESULT: CLI Command FAILED. " );
            Console.WriteLine ( $"EXCEPTION MESSAGE : {e.Message}" );
            Console.WriteLine ( $"INNER EXCEPTION MESSAGE : {e.InnerException?.Message ?? "NULL"}" );
        }
    }
    private static StringBuilder InitializeEntityFileBuilder( SyntaxList<UsingDirectiveSyntax> usings , FileScopedNamespaceDeclarationSyntax? @namespace )
    {
        var sb = new StringBuilder();
        sb.AppendLine ( $"// Generated from EF Core Tools " );
        sb.AppendLine ( "// Manual modification of files not recommended, could result in errors when using SqlService." );

        sb.AppendLine ( );
        sb.AppendLine ( );

        sb.AppendLine ( $"using {CommandParams.AtlConsultingIoProjects.ExigoGeneratorNamespaces.DatabaseEntities};" );
        foreach ( var u in usings )
            sb.AppendLine ( u.ToString ( ) );

        if ( @namespace is not null )
        {
            sb.AppendLine ( );
            sb.AppendLine ( @namespace.ToString ( ) );
        }

        sb.AppendLine ( );

        return sb;
    }
    private static Exception ContextFileNotFound( DbContextCommandArgs contextFileMeta )
    {
        return new Exception( $@" DbContext class file not found.  Search Path: '{ contextFileMeta.FileInfo.FullName }'");
    }
    private static Exception EntityFileNotFound( string searchDirectoryPath,  string sqlTableName )
        => new Exception ( $@"EntityFile not found for table {sqlTableName}.  Search Path: {searchDirectoryPath}\{sqlTableName}'" );

    public static string AttributeText( SqlTableMetadata tableMeta ) => "[ Table( " + tableMeta.Name.SurroundWithDoubleQuotes() + " ) ]";
    public const string SelectTablesWithPrimaryKeyDefinition = 
        @$"
                SELECT DISTINCT
                     {nameof(SqlTableMetadata.DatabaseName)} = t.TABLE_CATALOG
                    ,{nameof(SqlTableMetadata.SchemaName)} = t.TABLE_SCHEMA
                    ,{nameof(SqlTableMetadata.Name)} = t.TABLE_NAME    
                from INFORMATION_SCHEMA.TABLES t
                inner join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                on tc.TABLE_NAME = t.TABLE_NAME
                inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE u
                on u.TABLE_NAME = t.TABLE_NAME 
                and tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND t.TABLE_CATALOG = @databaseName
        ";
}
