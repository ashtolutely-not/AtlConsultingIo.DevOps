
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
            StringBuilder sb = GetBuilder( c.Namespace );

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
            StringBuilder sb = GetBuilder( c.Namespace );

            sb.AppendLine();
            sb.AppendLine( cls.ToString() );   
            
            string path = Path.Combine( handlerDir.FullName, cls.Identifier.ToString() + ".cs" );
            string file = sb.ToString();

            File.WriteAllText( path , file );
        }
    }



    private static StringBuilder GetBuilder( string? Namespace )
    {
        var sb = GetBuilder();
        if( !string.IsNullOrWhiteSpace( Namespace ))
            sb.AppendLine( Namespace );
        return sb;
    }   
    private static StringBuilder GetBuilder()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Code generated using Roslyn API's. ");
        sb.AppendLine("// Generator assumes decorated types are record structs with a property named Value that holds the framework provided value type.");
        sb.AppendLine("// Modification of these types after generation could have unintended consequences." );

        sb.AppendLine();
        return sb;
    }
    //Currently the logic assumes an empty constructor is available for all types, so we will limit scope to record structs for now
    private static ValueTypeCandidate? GetCandidate( FileInfo srcFile )
    {
        ValueTypeCandidate? candidate = null;
        string contents = File.ReadAllText( srcFile.FullName );
        if( string.IsNullOrWhiteSpace( contents ) )
            return candidate;

        SyntaxTree tree = CSharpSyntaxTree.ParseText( contents );
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        RecordDeclarationSyntax? fileRecord = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault();
        if( fileRecord is not RecordDeclarationSyntax _record || !_record.AttributeLists.Any() || !_record.IsKind(Kind.RecordStructDeclaration))
            return candidate;

        PropertyDeclarationSyntax? valueProp 
            = _record.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.ValueText.Equals("Value"));

        if( valueProp is not PropertyDeclarationSyntax _prop )
            return candidate;

        bool hasJsonAttr = _record.AttributeLists
            .SelectMany( a => a.Attributes )
            .FirstOrDefault( a => a.Name.ToString().Equals( NewtonsoftAttributeName ) ) is not null;

        bool hasDapperAttr = _record.AttributeLists
            .SelectMany( a => a.Attributes )
            .FirstOrDefault( a => a.Name.ToString().Equals( DapperAttributeName ) ) is not null;

        candidate = new( 
            _record, 
            _prop, 
            hasJsonAttr, 
            hasDapperAttr,
            GetNamespace(root));

        return candidate;
    }

    private static string? GetNamespace( CompilationUnitSyntax root )
    {
        NamespaceDeclarationSyntax? nsDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if( nsDeclaration is not null )
        {
            nsDeclaration = nsDeclaration.RemoveNodes( nsDeclaration.DescendantNodes().OfType<RecordDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia);
            if( nsDeclaration is not null )
                return nsDeclaration.ToFullString();
        }

        FileScopedNamespaceDeclarationSyntax? fileNsDeclaration 
            = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

        if( fileNsDeclaration is not null )
            fileNsDeclaration = fileNsDeclaration
                .RemoveNodes( fileNsDeclaration
                .DescendantNodes()
                .OfType<RecordDeclarationSyntax>(),SyntaxRemoveOptions.KeepNoTrivia);

        return fileNsDeclaration?.ToFullString();
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
        return $"{name}JsonConverter" ;
    }
    private static string HandlerClassIdentifier( ValueTypeCandidate candidate )
    {
        string name = candidate.Record.Identifier.ValueText;
        return $"{name}TypeHandler" ;
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
                            .AddParameterListParameters( Parameters.Reader, Parameters.ObjectType, Parameters.ExistingValueParam(candidate), Parameters.HasExisting, Parameters.Serializer )
                            .AddBodyStatements( MethodStatements.ReadJson( candidate ) )
                );

        public static MethodDeclarationSyntax WriteJson( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( Factory.ParseTypeName( "void" ), "WriteJson" )
                            .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                            .AddParameterListParameters( Parameters.Writer, Parameters.ValueParam(candidate), Parameters.Serializer )
                            .AddBodyStatements( MethodStatements.WriteJson )
                );

        public static MethodDeclarationSyntax Parse( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( ReturnType(candidate), "Parse" )
                        .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                        .AddParameterListParameters( Parameters.ObjectValueParam )
                        .AddBodyStatements( MethodStatements.Parse( candidate ) )
                );

        public static MethodDeclarationSyntax SetValue( ValueTypeCandidate candidate )
            => Format<MethodDeclarationSyntax>(
                    Factory.MethodDeclaration( Factory.ParseTypeName( "void" ), "SetValue" )
                        .AddModifiers( Factory.Token( Kind.PublicKeyword ), Factory.Token( Kind.OverrideKeyword ) )
                        .AddParameterListParameters( Parameters.DbParameter, Parameters.ValueParam(candidate) )
                        .AddBodyStatements( MethodStatements.SetValue )
                );
    }

    private static class Parameters
    {
        public static ParameterSyntax ValueParam( ValueTypeCandidate candidate )
        {
            string name = candidate.Record.Identifier.ValueText;
            return Factory.Parameter( Factory.Identifier( "value" ) )
                    .WithType( Factory.ParseTypeName( $"{name}" ) );
        }
        public static ParameterSyntax ExistingValueParam( ValueTypeCandidate candidate )
        {
            string name = candidate.Record.Identifier.ValueText;
            return Factory.Parameter( Factory.Identifier( "existingValue" ) )
                    .WithType( Factory.ParseTypeName( $"{name}" ) );
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
        public static StatementSyntax ReadJson( ValueTypeCandidate candidate )
        {
            string valueType = candidate.ValueProperty.Type.ToString();
            string typeName = candidate.Record.Identifier.ValueText;  
            
            return  Format<StatementSyntax>(
                    Factory.ParseStatement($"return serializer.Deserialize<{valueType}?>( reader ) is not {valueType} _value ? new {typeName}() : new {typeName}( _value );")
                );
        }
        public static readonly StatementSyntax WriteJson
            = Format<StatementSyntax>(Factory.ParseStatement($"serializer.Serialize( writer, value.Value );"));

        public static StatementSyntax Parse( ValueTypeCandidate candidate )
        {
            string valueType = candidate.ValueProperty.Type.ToString();
            string typeName = candidate.Record.Identifier.ValueText;    
            return Format<StatementSyntax>(
                    Factory.ParseStatement($"return value is not {valueType} _internal ? new {typeName}() : new {typeName}( _internal );")
                );
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
    string? Namespace );