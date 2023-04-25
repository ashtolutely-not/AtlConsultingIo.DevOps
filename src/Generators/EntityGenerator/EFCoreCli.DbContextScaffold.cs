
using AtlConsultingIo.DevOps;

using System.Text;
namespace AtlConsultingIo.Generators;
internal partial record struct EFCoreCliCommand
{
    public record struct DbContextScaffoldCommand
    {
        public const string Command = "dbcontext scaffold";
        private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
        private List<string> _args { get; init; }
        private List<string> _flags { get; init; }
        private List<string> _tableArgs { get; init; }
        private EFScaffoldConfiguration _configuration { get; init; }


        public DbContextScaffoldCommand( EFScaffoldConfiguration configuration )
        {
            _args = new();
            _flags = new();
            _tableArgs = new(); 

            _configuration = configuration;


        }
        public DbContextScaffoldCommand WithTableNames( List<string> tableNames )
        {
            var list = new List<string>();
            foreach( var tbl in tableNames )
                list.Add( string.Join( Utils.WhitespaceChar, Args.TableName, QualifiedName(tbl) ) );

            return this with { _tableArgs = list };
        }
        
        public DbContextScaffoldCommand WithConfiguration( EFScaffoldConfiguration configuration )
            => this with { _configuration = configuration };

        public string Build(  )
        {
            setArgs();
            setFlags();

            var cmdBldr = new StringBuilder( Command );

            cmdBldr.Append( _configuration.ConnectionString.SurroundWithDoubleQuotes().StartWithWhitespace() );
            cmdBldr.Append( SqlServerProvider.StartWithWhitespace() );

            foreach( var a in _args )
                cmdBldr.Append( a.StartWithWhitespace() );

            foreach( var t in _tableArgs )
                cmdBldr.Append( t.StartWithWhitespace() );

            foreach( var f in _flags )
                cmdBldr.Append( f.StartWithWhitespace() );

            return cmdBldr.ToString();
        }

        void setArgs()
        {
            _args.Clear();
            _args.Add( string.Join( Utils.WhitespaceChar, ContextNameArg , _configuration.ContextName ) );
            _args.Add( string.Join( Utils.WhitespaceChar, Args.EntityClassDirectory , _configuration.EntitiesOutDirectory.SurroundWithDoubleQuotes() ) );
            _args.Add( string.Join( Utils.WhitespaceChar, Args.EntityClassNamespace , _configuration.EntitiesNamespace ) );
            _args.Add( string.Join( Utils.WhitespaceChar, Args.DbContextClassDirectory , _configuration.ContextOutDirectory.SurroundWithDoubleQuotes() ) );
            _args.Add( string.Join( Utils.WhitespaceChar, Args.DbContextClassNamespace , _configuration.ContextNamespace ) );

            //var objDir =  Path.Join( DirectoryLocations.Projects.BuildOutputBase, ProjectNames.This, "obj").SurroundWithDoubleQuotes();
            //_args.Add( string.Join( TextUtils.WhitespaceChar, Args.MSBuildExtension, objDir ));

            //since the generator actually queries the database to get all of the eligible tables for the schema we don't want to set this
            //if the schema arg is included then all of the table args are ignored, meaning any ignored tables will still get created
            if( !_tableArgs.Any())
                _args.Add( string.Join( Utils.WhitespaceChar, Args.SchemaName , _configuration.Schema ) );
        }
        void setFlags()
        {
            _flags.Clear();
            _flags.Add( Flags.UseDataAnnotations );
            _flags.Add( Flags.ForceOverwrite );
            _flags.Add( Flags.NoOnConfiguringMethod );
            _flags.Add( NoBuildFlag );

            if( _configuration.UseExactNames )
                _flags.Add( Flags.UseDatabaseNames );

            if( !_configuration.UsePluralizer )
                _flags.Add( Flags.NoPluralizer );
        }
        string QualifiedName( string tableName ) => $"{ _configuration.Schema }.{ tableName }";
        internal struct Flags
        {
            public const string UseDataAnnotations = "--data-annotations";
            public const string UseDatabaseNames = "--use-database-names";
            public const string ForceOverwrite = "--force";
            public const string NoOnConfiguringMethod = "--no-onconfiguring";
            public const string NoPluralizer = "--no-pluralize";
        }
        internal struct Args
        {
            //Entity Args
            public const string EntityClassDirectory = "--output-dir";
            public const string EntityClassNamespace = "--namespace";

            //Context Args
            public const string DbContextClassDirectory = "--context-dir";
            public const string DbContextClassNamespace = "--context-namespace";

            //Constraints
            public const string SchemaName = "--schema";
            public const string TableName = "--table";

            public const string MSBuildExtension = "--msbuildprojectextensionspath";
        }

    }
}
