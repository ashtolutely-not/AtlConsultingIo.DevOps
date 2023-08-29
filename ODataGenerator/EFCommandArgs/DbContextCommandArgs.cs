using System.Text;

using AtlConsultingIo.DevOps.ODataGenerator;

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Primitives;

namespace AtlConsultingIo.Generators;

public record DbContextCommandArgs
{
    private const string Whitespace = " ";
    public string ContextTypeName  { get; init; }
    public string ContextNamespace { get; init; } 
    public string ContextDirectory { get; init; }
    public string OutPath { get; init; } 
    public FileInfo FileInfo => new ( Path.Combine( OutPath , ContextTypeName ) + ".cs" );

    public DbContextCommandArgs( ODataGeneratorSettings settings )
    {
        ContextTypeName = settings.DbContextName;
        ContextNamespace = settings.DbContextNamespace;

        DirectoryInfo parentDirectory = new( settings.EntityFrameworkProjectPath ?? settings.ODataProjectPath );

        ContextDirectory = settings.DbContextDirectoryName ?? parentDirectory.Name;
        OutPath = settings.DbContextDirectoryName.IsNullOrWhitespace() ? parentDirectory.FullName : Path.Combine( parentDirectory.FullName, settings.DbContextDirectoryName!);
    }


    private DbContextCommandArgs( )
    {
         ContextTypeName = ContextNamespace = ContextDirectory = OutPath = String.Empty;   
    }

    public static readonly DbContextCommandArgs Empty = new();
    public string GetCommandString( )
    {
        StringBuilder args = new( ScaffoldArgs.ContextName + Whitespace + ContextTypeName );
        args.Append( Whitespace );

        args.Append( ScaffoldArgs.OutDir + Whitespace + OutPath.SurroundWithDoubleQuotes()  );
        args.Append( Whitespace );

        args.Append( ScaffoldArgs.Namespace + Whitespace + ContextNamespace );

        return args.ToString();
    }

    internal struct ScaffoldArgs
    {
        public const string ContextName = "--context";
        public const string OutDir = "--context-dir";
        public const string Namespace = "--context-namespace";
    }
}
