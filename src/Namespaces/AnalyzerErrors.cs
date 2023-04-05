using Newtonsoft.Json;

namespace AtlConsultingIo.NamespaceAnalyzer;
internal static class AnalyzerErrors
{
    public static class ThrowIf
    {

        public static void InvalidJsonFile( string fileText )
        {
            if( JsonConvert.DeserializeObject<ProjectNamespaceOptions>( fileText ).IsEmpty )
                throw new InvalidDataException( $"Could not deserialize config file to valid NamespaceOptions instance." );
        }
        public static void ConfigFileNotFound( string configFileLocation )
        {
            var fileInfo = new FileInfo( configFileLocation );
            if( !fileInfo.Exists ) 
                throw new FileNotFoundException(null, configFileLocation);
        }

        public static void InvalidDocument( Document document )
        {
            if ( !document.SupportsSyntaxTree )
                throw new InvalidOperationException( $"File with unsupported syntax detected. {document.FilePath ?? document.Name}.  Stopping update operation." );

            if ( !document.SupportsSemanticModel )
                throw new InvalidOperationException( $"File with unsupported model detected. {document.FilePath ?? document.Name}.  Stopping update operation." );
        }



        public static void InvalidConfiguration( ProjectNamespaceOptions options )
        {
            var dir = new DirectoryInfo( options.TargetSourceLocation );
            if ( !dir.Exists )
                throw new ArgumentException( $"Source directory {options.TargetSourceLocation} does not exist." );

            var projectDirectories = dir.GetDirectories("*", SearchOption.AllDirectories );
            var toplevelFiles = dir.GetFiles( "*.cs", SearchOption.TopDirectoryOnly);


            foreach ( var config in options.DirectoryOptions )
            {
                var srcDir = projectDirectories.FirstOrDefault( d => d.Name.Equals( config.DirectoryName, StringComparison.OrdinalIgnoreCase));

                if ( srcDir is null )
                    throw new ArgumentException( $"Source directory {config.DirectoryName} does not exist." );
            }
        }

    }

}
