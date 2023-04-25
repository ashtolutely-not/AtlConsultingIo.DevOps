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

UpdateNamespaces();
static void UpdateNamespaces()
{
    var dir = new DirectoryInfo( Path.Combine(CommandParams.ProjectDirectoryPaths.AtlCore,"src"));
    var sourceDirs = dir.GetDirectories("*", SearchOption.TopDirectoryOnly);

    foreach( var folder in sourceDirs )
    {
        if( folder is null ) continue;
        if( folder.Name.Equals( "IntegrationServices" ) )
        {
            var subfolders = folder.GetDirectories("*",SearchOption.TopDirectoryOnly);
            foreach( var f in subfolders )
            {
                var nsLine = f.Name switch
                {
                    "Data" => "namespace AtlConsultingIo.Core.Data;",
                    "Http" => "namespace AtlConsultingIo.Core.Http;",
                    _ => "namespace AtlConsultingIo.Core;"
                };

                var srcFiles = f.GetFiles("*.cs", SearchOption.AllDirectories);
                foreach ( var file in srcFiles )
                {
                    var lines = File.ReadAllLines( file.FullName );
                    var sb = new StringBuilder();

                    foreach ( var ln in lines )
                    {
                        var newLine = !ln.StartsWith("namespace") ? ln : nsLine;
                        sb.AppendLine( newLine );
                    }
                    File.WriteAllText( file.FullName , sb.ToString() );
                }
            }
        }
        else
            UpdateNestedDirectories( folder );
    }

}

static void UpdateNestedDirectories( DirectoryInfo directory )
{
    var srcFiles = directory.GetFiles("*.cs", SearchOption.AllDirectories);
    var nsLine = "namespace AtlConsultingIo.Core;";
    foreach ( var file in srcFiles )
    {
        var lines = File.ReadAllLines( file.FullName );
        var sb = new StringBuilder();

        foreach ( var ln in lines )
        {
            var newLine = !ln.StartsWith("namespace") ? ln : nsLine;
            sb.AppendLine( newLine );
        }
        File.WriteAllText( file.FullName , sb.ToString() );
    }
}
static class ExigoEntitiesDebugBuild
{
    static async Task BuildExigoEntities( bool useTestDirectory )
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var projectConfig = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( cmdFile );

        if ( useTestDirectory )
            projectConfig = projectConfig with
            {
                ContextOutDirectory = Path.Combine( CommandParams.TestDirectoryPaths.GeneratedSqlEntitiesTest , "Contexts" ) ,
                EntitiesOutDirectory = Path.Combine( CommandParams.TestDirectoryPaths.GeneratedSqlEntitiesTest , "Exigo" )
            };

        await SqlEntityGenerator.Run( projectConfig , writeToFile: true );
        SqlEntityGenerator.AddSqlInterfaceSyntax( new DirectoryInfo( projectConfig.EntitiesOutDirectory ) );
    }
}



#pragma warning restore CS8321 // Local function is declared but never used