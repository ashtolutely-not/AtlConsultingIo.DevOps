#pragma warning disable CS8321 // Local function is declared but never used

#region Usings

using AtlConsultingIo.Generators;
using AtlConsultingIo.NamespaceAnalyzer;

using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;

#endregion


var dir = new DirectoryInfo( DirectoryLocations.Projects.Infrastructure);
var files = dir.GetFiles("*.cs", SearchOption.AllDirectories);

var outDir = Path.Combine( DirectoryLocations.LocalOutputs.BackupDirectory,"CoreNamespaceUpdates");
if ( !Directory.Exists( outDir ) )
    Directory.CreateDirectory( outDir );

foreach ( var file in files )
{
    var lines = File.ReadAllLines( file.FullName );
    var sb = new StringBuilder();

    foreach ( var ln in lines )
    {
        if ( ln.Contains( "namespace AtlConsultingIo.Core.Http;" ) )
            sb.AppendLine( "namespace AtlConsultingIo.Infrastructure.Http;" );
        else if ( ln.Contains( "namespace AtlConsultingIo.Core.Data;" ) )
            sb.AppendLine( "namespace AtlConsultingIo.Infrastructure.Data;" );
        else if ( ln.Contains( "namespace AtlConsultingIo.Core;" ) )
            sb.AppendLine( "namespace AtlConsultingIo.Infrastructure;" );
        else sb.AppendLine( ln );
    }

    File.WriteAllText( file.FullName , sb.ToString() );
}


static class ExigoEntitiesBuild
{
    static async Task BuildExigoEntities( bool useTestDirectory )
    {
        string cmdPath = FileLocations.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var projectConfig = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( cmdFile );

        if ( useTestDirectory )
            projectConfig = projectConfig with
            {
                ContextOutDirectory = Path.Combine( DirectoryLocations.LocalOutputs.GeneratedSqlEntitiesTest , "Contexts" ) ,
                EntitiesOutDirectory = Path.Combine( DirectoryLocations.LocalOutputs.GeneratedSqlEntitiesTest , "Exigo" )
            };

        await SqlEntityGenerator.Run( projectConfig , writeToFile: true );
        SqlEntityGenerator.AddSqlInterfaceSyntax( new DirectoryInfo( projectConfig.EntitiesOutDirectory ) );
    }
}



#pragma warning restore CS8321 // Local function is declared but never used