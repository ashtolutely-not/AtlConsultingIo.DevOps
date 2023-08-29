using System.Text;

using AtlConsultingIo.DevOps.ODataGenerator;

namespace AtlConsultingIo.Generators;

public record DbEntitiesCommandArgs
{
    private const string Whitespace = " ";
    public string EntitiesNamespace { get; init; } 
    public string EntititesDirectory { get; init; } 
    public string OutPath { get; init; } 

    public DbEntitiesCommandArgs( ODataGeneratorSettings settings )
    {
        EntitiesNamespace = settings.DbEntitiesNamespace;
        EntititesDirectory = settings.DbEntitiesDirectoryName;
        OutPath = Path.Combine ( ( settings.EntityFrameworkProjectPath ?? settings.ODataProjectPath ) , settings.DbEntitiesDirectoryName );
    }


    private DbEntitiesCommandArgs( )
    {
        EntitiesNamespace = EntititesDirectory  = OutPath = String.Empty;
    }

    public static readonly DbEntitiesCommandArgs Empty = new DbEntitiesCommandArgs();
    public string GetCommandString( )
    {
        StringBuilder args = new( ScaffoldArgs.OutDir + Whitespace + OutPath.SurroundWithDoubleQuotes() );
        args.Append ( Whitespace );
        args.Append ( ScaffoldArgs.Namespace + Whitespace + EntitiesNamespace );
        args.Append( Whitespace );

        return args.ToString ( );
    }

    internal struct ScaffoldArgs
    {
        public const string OutDir = "--output-dir";
        public const string Namespace = "--namespace";
    }
}
