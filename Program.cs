#pragma warning disable CS8321 // Local function is declared but never used
#region Usings

using AtlConsultingIo.Generators;
using Newtonsoft.Json;

using System.Text;
using AtlConsultingIo.DevOps;
using System.IO;



#endregion


////var buildProject = BuildProject.Create( CommandParams.TotalLifeProjects.DirectoryPaths.Integrations );
//var buildProject = BuildProject.Create( CommandParams.AtlConsultingIoProjects.DirectoryPaths.IntegrationOperations );

var buildProject = BuildProject.Create( CommandParams.TotalLifeProjects.DirectoryPaths.CheckoutService );
await BuildCommitAndPush( buildProject, BuildProfile.Debug );




static void Update( string directoryPath , string @namespace )
{
    var dir = new DirectoryInfo( directoryPath );
    var srcFiles = dir.GetFiles("*.cs", SearchOption.AllDirectories);
    foreach ( var item in srcFiles )
    {
        var lines = File.ReadAllLines( item.FullName ).ToList();
        var nsIndex = lines.FindIndex( l => l.StartsWith("namespace") && l.Trim().EndsWith(";"));
        if ( nsIndex == -1 )
            continue;

        lines[ nsIndex ] = $"namespace {@namespace};";
        File.WriteAllLines ( item.FullName , lines );
    }
}


static async Task BuildCommitAndPush( BuildProject project , BuildProfile profile )
{
    await BuildProjectCommands.CreateNewBuild( project , profile , updateVersion: false , validateResult: true );
    await BuildProjectCommands.AddCommitFiles( project );
    await BuildProjectCommands.CreateLocalCommitFromFile( project );
    await BuildProjectCommands.PushLocalCommits( project );
}

static async Task CreatePackageAndAddToLocalPackages( BuildProject buildProject , BuildProfile profile )
{
    string version = buildProject.GetNextVersion( );
    buildProject.UpdateProjectFile ( version );

    //Create a new nuget file
    await BuildProjectCommands.CreateLocalNugetPackage( buildProject , profile , true );
    BuildProjectCommands.CopyNugetFileToLocalPackages( buildProject , profile , version );

}




#pragma warning restore CS8321 // Local function is declared but never used

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

    }

    public static async Task ProjectBuild()
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var buildConfig = JsonConvert.DeserializeObject<EFScaffoldConfiguration>( cmdFile );

        await SqlEntityGenerator.Run( buildConfig );
        SqlEntityGenerator.AdjustNames( buildConfig );

    }
}