
using System.Data;
using System.Text;

using Factory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Kind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace AtlConsultingIo.DevOps;

internal static class ValueTypeGenerator
{
    private const string NewtonsoftAttributeName = @"NewtonsoftConverter";
    private const string DapperAttributeName = @"DapperHandler";

    private static readonly AdhocWorkspace _workspace = new();

    public static void Run( string sourceDirectoryPath, string outputDirectoryPath )
    {
        DirectoryInfo src = new( sourceDirectoryPath );
        if( !src.Exists )
            throw new DirectoryNotFoundException( sourceDirectoryPath );

        DirectoryInfo output = new( outputDirectoryPath );
        if( !output.Exists )
            output.Create();

        FileInfo[] files = src.GetFiles( "*.cs" , SearchOption.AllDirectories );
        List<ValueTypeCandidate> candidates = new List<ValueTypeCandidate>();
        foreach ( FileInfo srcFile in files )
            if ( GetCandidate( srcFile ) is ValueTypeCandidate candidate )
                candidates.Add( candidate );

        if( !candidates.Any() )
            return;

        GenerateConverters( candidates , output );
        GenerateHandlers( candidates , output );
    }


    private static void GenerateConverters( IEnumerable<ValueTypeCandidate> candidates , DirectoryInfo outputDirectory )
    {
        var _candidates = candidates.Where( c => c.HasNewtonsoftAttribute );
        if( !_candidates.Any() )
            return;

        DirectoryInfo converterDir = ConverterDirectory( outputDirectory );
        foreach( var c in _candidates )
        {
            ClassDeclarationSyntax cls = ConstructConverter( c );
            StringBuilder sb = c switch
            {
                ValueTypeCandidate { Namespace : not null } candidate => new( candidate.Namespace.ToFullString() ),
                ValueTypeCandidate { FileNamespace : not null } candidate => new( candidate.FileNamespace.ToFullString() ),
                _ => new()
            };

            sb.AppendLine();
            sb.AppendLine( cls.ToString() );   
            
            string path = Path.Combine( converterDir.FullName, cls.Identifier.ToString() + ".cs" );
            string file = sb.ToString();

            File.WriteAllText( path , file );
        }
    }

    private static DirectoryInfo ConverterDirectory( DirectoryInfo outputDirectory )
    {
        string dirName = "NewtonsoftConverters";
        DirectoryInfo converterDir = new DirectoryInfo(Path.Combine(outputDirectory.FullName, dirName));

        if( converterDir.Exists && converterDir.CSharpFiles().Any() )
            converterDir.Delete( true );

        return converterDir.Exists ? converterDir : outputDirectory.CreateSubdirectory( dirName );

    }
    private static DirectoryInfo HandlerDirectory( DirectoryInfo outputDirectory )
    {
        string dirName = "DapperHandlers";
        DirectoryInfo handlerDir = new DirectoryInfo(Path.Combine(outputDirectory.FullName, dirName));

        if( handlerDir.Exists && handlerDir.CSharpFiles().Any() )
            handlerDir.Delete( true );

        return handlerDir.Exists ? handlerDir : outputDirectory.CreateSubdirectory( dirName );
    }   
    private static void GenerateHandlers( IEnumerable<ValueTypeCandidate> candidates, DirectoryInfo outputDirectory )
    {
        var _candidates = candidates.Where( c => c.HasDapperAttribute );
        if( !_candidates.Any() )
            return;

        DirectoryInfo handlerDir = HandlerDirectory( outputDirectory );
        foreach( var c in _candidates )
        {
            ClassDeclarationSyntax cls = ConstructHandler( c );
            StringBuilder sb = c switch
            {
                ValueTypeCandidate { Namespace : not null } candidate => new( candidate.Namespace.ToFullString() ),
                ValueTypeCandidate { FileNamespace : not null } candidate => new( candidate.FileNamespace.ToFullString() ),
                _ => new()
            };

            sb.AppendLine();
            sb.AppendLine( cls.ToString() );   
            
            string path = Path.Combine( handlerDir.FullName, cls.Identifier.ToString() + ".cs" );
            string file = sb.ToString();

            File.WriteAllText( path , file );
        }
    }

    private static ValueTypeCandidate? GetCandidate( FileInfo srcFile )
    {
        ValueTypeCandidate? candidate = null;
        string contents = File.ReadAllText( srcFile.FullName );
        if( string.IsNullOrWhiteSpace( contents ) )
            return candidate;

        SyntaxTree tree = CSharpSyntaxTree.ParseText( contents );
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        RecordDeclarationSyntax? fileRecord = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault();
        if( fileRecord is not RecordDeclarationSyntax _record || !_record.AttributeLists.Any())
            return candidate;

        PropertyDeclarationSyntax? valueProp 
            = _record.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.ValueText.Equals("Value"));

        if( valueProp is not PropertyDeclarationSyntax _prop )
            return candidate;

        NamespaceDeclarationSyntax? nsDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        FileScopedNamespaceDeclarationSyntax? fileNsDeclaration = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

        NamespaceDeclarationSyntax? cleanedNs 
            = nsDeclaration is NamespaceDeclarationSyntax _ns ?
            _ns.RemoveNodes( _ns.DescendantNodes().OfType<RecordDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia) : null;

        FileScopedNamespaceDeclarationSyntax? cleanedFileNs 
            = fileNsDeclaration is FileScopedNamespaceDeclarationSyntax _fileNs ?
            _fileNs.RemoveNodes( _fileNs.DescendantNodes().OfType<RecordDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia) : null;

        candidate = new( 
            _record, 
            _prop, 
            HasNewtonsoftAttribute( _record ), 
            HasDapperAttribute( _record ) ,
            cleanedNs,
            cleanedFileNs);

        return candidate;
    }
    private static bool HasNewtonsoftAttribute( RecordDeclarationSyntax record )
    {
        var attributes = record.AttributeLists
            .SelectMany( a => a.Attributes );

        AttributeSyntax? attribute 
            = attributes.FirstOrDefault( a => a.Name.ToString().Equals( NewtonsoftAttributeName ) );

        return attribute is not null;
    }
    private static bool HasDapperAttribute( RecordDeclarationSyntax record )
    {
        var attributes = record.AttributeLists
            .SelectMany( a => a.Attributes );

        AttributeSyntax? attribute 
            = attributes.FirstOrDefault( a => a.Name.ToString().Equals( DapperAttributeName ) );

        return attribute is not null;
    }

    private static ClassDeclarationSyntax ConstructConverter( ValueTypeCandidate candidate )
        => Format<ClassDeclarationSyntax>(
                Factory.ClassDeclaration( ConverterClassIdentifier(candidate) )
                        .AddModifiers( Factory.Token( Kind.PublicKeyword ) , Factory.Token( Kind.SealedKeyword ) )
                        .AddBaseListTypes( NewtonsoftBaseType( candidate ) )
                        .AddMembers( Methods.ReadJson( candidate ) , Methods.WriteJson( candidate ) )
            );
    private static ClassDeclarationSyntax ConstructHandler( ValueTypeCandidate candidate )
        => Format<ClassDeclarationSyntax>(
                Factory.ClassDeclaration( HandlerClassIdentifier(candidate) )
                .AddModifiers( Factory.Token( Kind.PublicKeyword ) , Factory.Token( Kind.SealedKeyword ) )
                .AddBaseListTypes( DapperBaseType( candidate ) )
                .AddMembers( Methods.Parse( candidate ) , Methods.SetValue( candidate ) )
            );

    private static string ConverterClassIdentifier( ValueTypeCandidate candidate )
    {
        string name = candidate.Record.Identifier.ValueText;
        return $"{name}NewtonsoftConverter" ;
    }
    private static string HandlerClassIdentifier( ValueTypeCandidate candidate )
    {
        string name = candidate.Record.Identifier.ValueText;
        return $"{name}DapperHandler" ;
    }

    private static SimpleBaseTypeSyntax DapperBaseType( ValueTypeCandidate candidate )
    {
        string name = candidate.Record.Identifier.ValueText;
        return Format<SimpleBaseTypeSyntax>(Factory.SimpleBaseType( Factory.ParseTypeName( $"Dapper.SqlMapper.TypeHandler<{name}>" ) ));
    }
    private static SimpleBaseTypeSyntax NewtonsoftBaseType( ValueTypeCandidate candidate )
    {
        string name = candidate.Record.Identifier.ValueText;
        return Format<SimpleBaseTypeSyntax>(Factory.SimpleBaseType( Factory.ParseTypeName( $"Newtonsoft.Json.JsonConverter<{name}>" ) ));
    } 

    private static T Format<T>( T value ) where T : SyntaxNode
    {
        return (T)Formatter.Format( value, _workspace );
    }
    private static class Methods
    {
        public static TypeSyntax ReturnType( ValueTypeCandidate candidate )
        {
            string name = candidate.Record.Identifier.ValueText;
            return Factory.ParseTypeName( $"{name}" );
        }

        public static MethodDeclarationSyntax ReadJson( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( ReturnType(candidate), "ReadJson")
                            .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                            .AddParameterListParameters( MethodParams.Reader, MethodParams.ObjectType, MethodParams.ExistingValueParam(candidate), MethodParams.HasExisting, MethodParams.Serializer )
                            .AddBodyStatements( MethodStatements.ReadJson( candidate ) )
                );

        public static MethodDeclarationSyntax WriteJson( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( Factory.ParseTypeName( "void" ), "WriteJson" )
                            .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                            .AddParameterListParameters( MethodParams.Writer, MethodParams.ValueParam(candidate), MethodParams.Serializer )
                            .AddBodyStatements( MethodStatements.WriteJson )
                );

        public static MethodDeclarationSyntax Parse( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( ReturnType(candidate), "Parse" )
                        .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                        .AddParameterListParameters( MethodParams.ObjectValueParam )
                        .AddBodyStatements( MethodStatements.Parse( candidate ) )
                );

        public static MethodDeclarationSyntax SetValue( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( Factory.ParseTypeName( "void" ), "SetValue" )
                        .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                        .AddParameterListParameters( MethodParams.DbParameter, MethodParams.ValueParam(candidate) )
                        .AddBodyStatements( MethodStatements.SetValue )
                );
    }

    private static class MethodParams
    {
        public static ParameterSyntax ValueParam( ValueTypeCandidate candidate )
        {
            string name = candidate.Record.Identifier.ValueText;
            return Factory.Parameter( Factory.Identifier( "value" ) )
                    .WithType( Factory.ParseTypeName( $"{name}?" ) );
        }
        public static ParameterSyntax ExistingValueParam( ValueTypeCandidate candidate )
        {
            string name = candidate.Record.Identifier.ValueText;
            return Factory.Parameter( Factory.Identifier( "existingValue" ) )
                    .WithType( Factory.ParseTypeName( $"{name}?" ) );
        }
        public static readonly ParameterSyntax Reader 
            = Factory.Parameter( Factory.Identifier( "reader" ) )
              .WithType( Factory.ParseTypeName( "Newtonsoft.Json.JsonReader" ) );

        public static readonly ParameterSyntax ObjectType
            = Factory.Parameter( Factory.Identifier( "objectType" ) )
              .WithType( Factory.ParseTypeName( "Type" ) );

        public static readonly ParameterSyntax HasExisting
            = Factory.Parameter( Factory.Identifier( "hasExistingValue" ) )
              .WithType( Factory.ParseTypeName( "bool" ) );

        public static readonly ParameterSyntax Serializer 
            = Factory.Parameter( Factory.Identifier( "serializer" ) )
              .WithType( Factory.ParseTypeName( "Newtonsoft.Json.JsonSerializer" ) );

        public static readonly ParameterSyntax Writer 
            = Factory.Parameter( Factory.Identifier( "writer" ) )
              .WithType( Factory.ParseTypeName( "Newtonsoft.Json.JsonWriter" ) );

        public static readonly ParameterSyntax DbParameter 
            = Factory.Parameter( Factory.Identifier( "parameter" ) )
              .WithType( Factory.ParseTypeName( "System.Data.IDbDataParameter" ) );

        public static readonly ParameterSyntax ObjectValueParam
            = Factory.Parameter( Factory.Identifier( "value" ) )
              .WithType( Factory.ParseTypeName( "object" ) );
    }

    private static class MethodStatements
    {
        public static StatementSyntax[] ReadJson( ValueTypeCandidate candidate )
        {
            string valueType = candidate.ValueProperty.Type.ToString();
            string typeName = candidate.Record.Identifier.ValueText;  
            
            return new StatementSyntax[]
            {
                Format<StatementSyntax>(Factory.ParseStatement($"{valueType}? value = serializer.Deserialize<{valueType}?>( reader );")),
                Format<StatementSyntax>(Factory.ParseStatement($"return new {typeName}( value ?? default );"))
            };
        }

        public static readonly StatementSyntax WriteJson
            = Format<StatementSyntax>(Factory.ParseStatement($"serializer.Serialize( writer, value.Value );"));

        public static StatementSyntax[] Parse( ValueTypeCandidate candidate )
        {
            string valueType = candidate.ValueProperty.Type.ToString();
            string typeName = candidate.Record.Identifier.ValueText;    
            return new StatementSyntax[]
            {
                Format<StatementSyntax>(
                    Factory.ParseStatement($"return value is not {valueType} _internal ? {typeName}.Default : new {typeName}( _internal );")
                    )
            };
        }

        public static readonly StatementSyntax SetValue
            = Format<StatementSyntax>(Factory.ParseStatement($"parameter.Value = value.Value;"));
    }
}


internal readonly record struct ValueTypeCandidate( 
    RecordDeclarationSyntax Record , 
    PropertyDeclarationSyntax ValueProperty , 
    bool HasNewtonsoftAttribute, 
    bool HasDapperAttribute,
    NamespaceDeclarationSyntax? Namespace = null ,
    FileScopedNamespaceDeclarationSyntax? FileNamespace = null );