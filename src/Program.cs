#pragma warning disable CS8321 // Local function is declared but never used

#region Usings
using CliWrap.Buffered;
using CliWrap;
using AtlConsultingIo.Generators;
using AtlConsultingIo.NamespaceAnalyzer;

using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;
using AtlConsultingIo.DevOps;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;
using System.IO;

#endregion


await ExigoEntitiesBuild.ProjectBuild();

static class NamespaceUpdates
{
    public static void UpdateAtlCoreNamespaces()
    {
        DirectoryInfo dir = new ( Path.Combine(CommandParams.ProjectDirectoryPaths.Atl_Core,"src") );
        DirectoryInfo[] sourceDirs = dir.GetDirectories("*", SearchOption.TopDirectoryOnly);

        foreach( DirectoryInfo folder in sourceDirs )
        {
            Action<DirectoryInfo> func = folder.Name switch
            {
                "Logging" => SetToAtlCoreLogging,
                "Data" => SetToAtlCoreData,
                "Http" => SetToAtlCoreHttp,
                _ => SetToAtlCore
            };

            func( folder );
        }

    }
    public static void SetToAtlCoreLogging( DirectoryInfo directory )
    {
        if(!directory.Exists) return;
        var srcFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
        foreach ( var file in srcFiles )
        {
            var lines = File.ReadAllLines( file.FullName );
            var sb = new StringBuilder();

            foreach ( var ln in lines )
            {
                var newLine = !ln.StartsWith("namespace") ? ln : CommandParams.NamespaceStatements.Atl_Core_Logging;
                sb.AppendLine( newLine );
            }
            File.WriteAllText( file.FullName , sb.ToString() );
        }
    }
    public static void SetToAtlCoreHttp( DirectoryInfo directory )
    {
        if(!directory.Exists) return;
        var srcFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
        foreach ( var file in srcFiles )
        {
            var lines = File.ReadAllLines( file.FullName );
            var sb = new StringBuilder();

            foreach ( var ln in lines )
            {
                var newLine = !ln.StartsWith("namespace") ? ln : CommandParams.NamespaceStatements.Atl_Core_Http;
                sb.AppendLine( newLine );
            }
            File.WriteAllText( file.FullName , sb.ToString() );
        }
    }
    public static void SetToAtlCoreData( DirectoryInfo directory )
    {
        if(!directory.Exists) return;
        var srcFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
        foreach ( var file in srcFiles )
        {
            var lines = File.ReadAllLines( file.FullName );
            var sb = new StringBuilder();

            foreach ( var ln in lines )
            {
                var newLine = !ln.StartsWith("namespace") ? ln : CommandParams.NamespaceStatements.Atl_Core_Data;
                sb.AppendLine( newLine );
            }
            File.WriteAllText( file.FullName , sb.ToString() );
        }
    }
    public static void SetToAtlCore( DirectoryInfo directory )
    {
        if(!directory.Exists) return;
        var srcFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
        foreach ( var file in srcFiles )
        {
            var lines = File.ReadAllLines( file.FullName );
            var sb = new StringBuilder();

            foreach ( var ln in lines )
            {
                var newLine = !ln.StartsWith("namespace") ? ln : CommandParams.NamespaceStatements.Atl_Core;
                sb.AppendLine( newLine );
            }
            File.WriteAllText( file.FullName , sb.ToString() );
        }
    }
}

static class ExigoEntitiesBuild
{
    public static async Task TestBuild()
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var buildConfig = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( cmdFile ) with
        {
            ContextOutDirectory = Path.Combine( CommandParams.TestDirectoryPaths.ExigoEntitiesTests , "dbo", "Context" ) ,
            EntitiesOutDirectory = Path.Combine( CommandParams.TestDirectoryPaths.ExigoEntitiesTests , "dbo", "Entities" )
        };

        await SqlEntityGenerator.Run( buildConfig );
        SqlEntityGenerator.AdjustNames( buildConfig );
        SqlEntityGenerator.AddSqlInterfaceSyntax( buildConfig );  
    }

    public static async Task ProjectBuild()
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var buildConfig = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( cmdFile );

        await SqlEntityGenerator.Run( buildConfig );
        SqlEntityGenerator.AdjustNames( buildConfig );
        SqlEntityGenerator.AddSqlInterfaceSyntax( buildConfig );  
    }
}



#pragma warning restore CS8321 // Local function is declared but never used