#pragma warning disable CS8321 // Local function is declared but never used
#region Usings

using AtlConsultingIo.Generators;
using Newtonsoft.Json;

using System.Text;
using AtlConsultingIo.DevOps;
using System.IO;
using Microsoft.CodeAnalysis;
using System;
using AtlConsultingIo.DevOps.ODataGenerator;



#endregion

//var core = BuildProject.Create( CommandParams.TotalLifeProjects.DirectoryPaths.Core );
//var checkout = BuildProject.Create( CommandParams.TotalLifeProjects.DirectoryPaths.CheckoutService);

//await CreatePackageAndAddToLocalPackages( checkout , BuildProfile.Debug );
//UpdateNamespaces.Run();

await ExigoEntitiesBuild.TestBuild();

static async Task BuildCommitAndPush( BuildProject project , BuildProfile profile )
{
    await BuildProjectCommands.CreateNewBuild ( project , profile , updateVersion: false , validateResult: true );
    await BuildProjectCommands.AddCommitFiles ( project );
    await BuildProjectCommands.CreateLocalCommitFromFile ( project );
    await BuildProjectCommands.PushLocalCommits ( project );
}

static async Task CreatePackageAndAddToLocalPackages( BuildProject buildProject , BuildProfile profile )
{
    string version = buildProject.GetNextVersion( );
    buildProject.UpdateProjectFile ( version );

    //Create a new nuget file
    await BuildProjectCommands.CreateLocalNugetPackage ( buildProject , profile , true );
    BuildProjectCommands.CopyNugetFileToLocalPackages ( buildProject , profile , version );

}




#pragma warning restore CS8321 // Local function is declared but never used

static class ExigoEntitiesBuild
{
    public static async Task TestBuild( )
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );

        var buildConfig = JsonConvert.DeserializeObject<ODataGeneratorSettings>( cmdFile ) with
        {
            EntityFrameworkProjectPath = Path.Combine( CommandParams.TestDirectoryPaths.ExigoEntitiesTests , "dbo") ,
            DbContextDirectoryName = "Context",
            DbEntitiesDirectoryName =  "Entities" 
        };

        await ControllerGenerator.GenerateEntities ( buildConfig );

    }
    public static async Task ProjectBuild( )
    {
        string cmdPath = CommandParams.FilePaths.ExigoSqlEntitiesConfig;
        string cmdFile = File.ReadAllText( cmdPath );
        var buildConfig = JsonConvert.DeserializeObject<ODataGeneratorSettings>( cmdFile );
        await ControllerGenerator.GenerateEntities ( buildConfig );

    }
}

static class UpdateNamespaces
{
    //the code here is always going to be throw away code once a project gets to a certain point
    public static void Run( )
    {
        var buildProject = BuildProject.Create( CommandParams.TotalLifeProjects.DirectoryPaths.ShoppingApi );
        var filesToUpdate = buildProject.GetSourceFiles( null );

        CleanAndUpdateNamespaces( filesToUpdate, "TotalLife.ShoppingApi", new List<string>{ "TotalLife.Api." } );
    }
    static void UpdateOperationsProject( List<string> coreNamespaces )
    {
        var operationsNamespaces = new List<string>();

        var opsProj = BuildProject.Create("C:\\Users\\ashto\\source\\repos\\AtlConsultingIo\\AtlConsultingIo.CompanyOperations\\CompanyName.Operations");
        var opsNs = "CompanyName.Operations";
        var usingsToStrip = new List<string>(){ "CompanyName" };
        CleanAndUpdateTopLevelFiles ( opsProj.ProjectDirectoryInfo , opsNs, usingsToStrip );
        operationsNamespaces.Add ( opsNs );

        var subDirectories = opsProj.ProjectDirectoryInfo.GetDirectories ( )?.ToList ( ) ?? new();
        foreach ( var dir in subDirectories )
        {
            string ns = IsDomainDirectory( dir ) ? opsNs + "." + dir.Name : opsNs;
            CleanAndUpdateSubdirectories ( dir , ns , usingsToStrip );
            if ( !operationsNamespaces.Contains ( ns ) )
                operationsNamespaces.Add ( ns );
        }

        operationsNamespaces.AddRange( coreNamespaces );
        AddUsings( opsProj, operationsNamespaces );
    }
    static List<string> UpdateGenericCoreProject( )
    {
        BuildProject coreProj = BuildProject.Create( "C:\\Users\\ashto\\source\\repos\\AtlConsultingIo\\AtlConsultingIo.CompanyOperations\\CompanyName.Core" );
        string baseNamespace = "CompanyName.Core";

        var coreNamespaces = new List<string>();
        var usingsToStrip = new List<string>(){ "CompanyName" };
        CleanAndUpdateTopLevelFiles ( coreProj.ProjectDirectoryInfo , baseNamespace , usingsToStrip );
        coreNamespaces.Add ( baseNamespace );

        coreNamespaces.AddRange( UpdateCoreEntititesDirectory ( coreProj , baseNamespace , usingsToStrip ) );
        coreNamespaces.AddRange( UpdateCoreIntegrationsDirectory ( coreProj , baseNamespace, usingsToStrip ) );

        AddUsings( coreProj, coreNamespaces );
        return coreNamespaces;
    }
    static void AddUsings( BuildProject project , List<string> namespaces )
    {
        var projectFiles = project.ProjectDirectoryInfo.GetFiles("*.cs",SearchOption.AllDirectories);
        var usings = namespaces.Select( str => $"using {str};")?.ToList() ?? new();
        foreach ( var file in projectFiles )
        {
            var lines = File.ReadAllLines( file.FullName);

            var newLines = lines.Where( l => l.StartsWith("//"))?.ToList() ?? new();
            newLines.AddRange ( usings );

            foreach ( var line in lines )
                if ( !line.StartsWith ( "//" ) && !line.Trim ( ).StartsWith ( "using TotalLife." ) )
                    newLines.Add ( line );

            File.WriteAllLines ( file.FullName , newLines );
        }
    }
    static List<string> UpdateCoreEntititesDirectory( BuildProject coreProj , string baseNamespace , List<string> usingsToStrip )
    {
        string entitiesNamespace = string.Join(".", baseNamespace, "Entities");
        var entitiesDirectory = new DirectoryInfo(Path.Combine( coreProj.ProjectDirectoryInfo.FullName, "Entities"));
        
        CleanAndUpdateTopLevelFiles ( entitiesDirectory , entitiesNamespace, usingsToStrip );
        var dirNamespaces = new List<string> { entitiesNamespace };

        var subDirectories = entitiesDirectory.GetDirectories()?.ToList() ?? new();
        foreach ( var dir in subDirectories )
        {
            string ns = IsDomainDirectory( dir ) ? entitiesNamespace + "." + dir.Name : entitiesNamespace;
            CleanAndUpdateSubdirectories ( dir , ns , usingsToStrip );
            if ( !dirNamespaces.Contains ( ns ) )
                dirNamespaces.Add ( ns );
        }

        return dirNamespaces;
    }
    static List<string> UpdateCoreIntegrationsDirectory( BuildProject coreProj, string baseNamespace , List<string> usingsToStrip )
    {
        var integrationsDir = new DirectoryInfo(Path.Combine( coreProj.ProjectDirectoryInfo.FullName,"Integrations"));
        var integrationsNamespace = string.Join ( "." , baseNamespace , "Integrations" );

        CleanAndUpdateTopLevelFiles ( integrationsDir , integrationsNamespace, usingsToStrip );
        var dirNamespaces = new List<string> { integrationsNamespace };

        var subDirectories = integrationsDir.GetDirectories ( )?.ToList ( ) ?? new();
        foreach ( var dir in subDirectories )
        {
            var dirNs = integrationsNamespace + "." + dir.Name;
            if ( !dir.Name.Equals ( "Exigo" ) )
            {
                CleanAndUpdateSubdirectories ( dir , dirNs, usingsToStrip );
                if ( !dirNamespaces.Contains ( dirNs ) )
                    dirNamespaces.Add ( dirNs );
                continue;
            }
            else
            {
                var exigoDirectories = dir.GetDirectories()?.ToList() ?? new();
                foreach ( var exigoDir in exigoDirectories )
                {
                    string exigNs = exigoDir.Name switch
                    {
                        "Rest" => dirNs + "." + exigoDir.Name,
                        "Sql" => dirNs + "." + exigoDir.Name,
                        _ => dirNs
                    };

                    CleanAndUpdateSubdirectories ( exigoDir , exigNs , usingsToStrip );
                    if ( !dirNamespaces.Contains ( exigNs ) )
                        dirNamespaces.Add ( exigNs );
                }
            }

        }


        return dirNamespaces;
    }
    static bool IsDomainDirectory( DirectoryInfo directory ) => !new List<string>
    {
        "_Shared",
        "Options",
        "Results"
    }.Contains ( directory.Name );
    static void CleanAndUpdateSubdirectories( DirectoryInfo topLevelDirectory , string newNamespace , List<string> usingsToStrip , bool appendDirectoryToNamespace = false )
    {
        var files = topLevelDirectory.GetFiles("*.cs", SearchOption.TopDirectoryOnly)?.ToList() ?? new List<FileInfo>();
        CleanAndUpdateNamespaces ( files , newNamespace , usingsToStrip );

        var subDirectories = topLevelDirectory.GetDirectories()?.ToList() ?? new List<DirectoryInfo>();
        if( subDirectories.Count == 0 )
            return;

        for ( int i = 0 ; i < subDirectories.Count ; i++ )
        {
            DirectoryInfo dir = subDirectories[i];
            string ns = appendDirectoryToNamespace ? string.Join('.',newNamespace, dir.Name ) : newNamespace;
            CleanAndUpdateNamespaces( dir.SourceFilesInThisDirectory() , ns, usingsToStrip );
        }
    }
    static void CleanAndUpdateTopLevelFiles( DirectoryInfo directory , string newNamespace , List<string> usingsToStrip )
    {
        var files = directory.GetFiles("*.cs", SearchOption.TopDirectoryOnly)?.ToList() ?? new List<FileInfo>();
        CleanAndUpdateNamespaces ( files , newNamespace, usingsToStrip);
    }
    static void CleanAndUpdateNamespaces( List<FileInfo> filesToUpdate , string newNamespace , List<string> usingsToStrip )
    {
        foreach ( var f in filesToUpdate )
        {
            var lines = File.ReadAllLines( f.FullName );
            var newLines = new List<string>();

            foreach ( var line in lines )
            {
                bool skip = false;
                for ( int i = 0; i < usingsToStrip.Count; i++ )
                {
                    if( !line.StartsWith($"using {usingsToStrip[i]}") )
                        continue;

                    skip = true;
                    break;
                }
                if ( skip.Equals(true) )
                    continue;

                Action add = IsReplacementLine( line , newNamespace ) 
                                    ? () =>  newLines.Add( NewNamespaceLine( newNamespace ) )
                                    : () => newLines.Add( line );

                add();

            }

            File.WriteAllLines ( f.FullName , newLines );
        }
    }
    static string NewNamespaceLine( string newNamespace ) => $"namespace {newNamespace};";
    static bool IsReplacementLine( string line , string newNamespace ) => IsNamespaceLine( line ) && !NamespaceIsCurrent( line, newNamespace );
    static bool IsNamespaceLine( string line ) => line.StartsWith ( "namespace" );
    static bool NamespaceIsCurrent( string line, string newNamespace ) => line.Trim ( ).Equals ( NewNamespaceLine(newNamespace) );
}