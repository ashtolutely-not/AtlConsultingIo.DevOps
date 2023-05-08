
using AtlConsultingIo.DevOps;

using System.Text;

namespace AtlConsultingIo.Generators;
internal static class ExigoJsonGenerator
{
    static readonly List<string> Exclusions = new(){ "Resource", "Release", "Transactional","ExigoApiClient" , "CultureCodeItem" };
    static readonly Workspace _ws = new AdhocWorkspace();

    static readonly StringBuilder _enumFile = InitializeFileBuilder();
    static readonly StringBuilder _interfaceFile = InitializeFileBuilder();
    static readonly List<RecordDeclarationSyntax> _updatedRecords = new List<RecordDeclarationSyntax>();

    static readonly List<string> _nullableTypeNames= new List<string>{ "DataTable", "DataSet", "object", "ITransactionMember" };
    static Compilation _comp { get; set; } = CSharpCompilation.Create( null );

    public static void InspectSourceFiles()
    {
        var srcDirectory = new DirectoryInfo( CommandParams.ExigoClientSourcePath );
        var srcFiles = srcDirectory.GetFiles().ToList();

        var list = new List<string>();
        foreach ( var file in srcFiles )
        {
            string fileTxt = File.ReadAllText( file.FullName );

            SyntaxTree tree = CSharpSyntaxTree.ParseText( fileTxt );
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            if ( root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Any() )
                list.AddRange( root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Select( i => i.Identifier.ValueText ) );

        }

        foreach ( var itm in list )
            Console.WriteLine( itm );
    }
    public static void Run( bool writeToProject )
    {
        Reset();

        var outDirectory = !Directory.Exists( OutPath( writeToProject ) ) ?
                        Directory.CreateDirectory( OutPath( writeToProject ) ) :
                        new DirectoryInfo( OutPath( writeToProject ) );

        var srcDirectory = new DirectoryInfo( CommandParams.ExigoClientSourcePath );
        var srcFiles = srcDirectory.GetFiles().ToList();

        InitializeNullableTypeNames( srcFiles );
        srcFiles.ForEach( f => RewriteSyntax( f ) );

        WriteEnumFile( outDirectory );
        WriteInterfaceFile( outDirectory );
        WriteRecordFiles( outDirectory );
    }


    static void Reset()
    {
        _updatedRecords.Clear();
        _enumFile.Clear();
        _interfaceFile.Clear();
    }
    static void RewriteSyntax( FileInfo srcFile )
    {
        string fileTxt = File.ReadAllText( srcFile.FullName );

        SyntaxTree tree = CSharpSyntaxTree.ParseText( fileTxt );
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var records = GetRecordDeclarations( tree, root );
        if ( records.Any() )
            _updatedRecords.AddRange( records );

        var enums = GetEnumDeclarations( root );
        foreach ( var e in enums )
            AppendEnumToFile( e );

        var interfaces = GetInterfaceDeclarations( root );
        foreach ( var i in interfaces )
            AppendInterfaceToFile( i );
    }

    static void InitializeNullableTypeNames( List<FileInfo> sourceFiles )
    {
        foreach( var file in sourceFiles )
        {
            string fileTxt = File.ReadAllText( file.FullName );

            SyntaxTree tree = CSharpSyntaxTree.ParseText( fileTxt );
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            var classes = root
                .DescendantNodes()
                .Where( n => n is ClassDeclarationSyntax _cls)
                .Select( n => (ClassDeclarationSyntax)n);

            if( classes is not null && classes.Any())
                foreach( var cls in classes )
                    if( !cls.Ignore() )
                    {
                        var clsName = cls.Identifier.ToString().Trim();

                        //check for a fully qualified name
                        var nameParts = clsName.Split( '.' );
                        _nullableTypeNames.Add( nameParts.Count().Equals(1) ? clsName : nameParts[^1] );
                    }
        }
    }

    static IEnumerable<RecordDeclarationSyntax> GetRecordDeclarations( SyntaxTree tree , CompilationUnitSyntax root )
    {
        var classes = root
                        .DescendantNodes()
                        .Where( n => n is ClassDeclarationSyntax _cls)
                        .Select( n => (ClassDeclarationSyntax)n);

        if ( classes is null )
            return Enumerable.Empty<RecordDeclarationSyntax>();

        _comp = CSharpCompilation.Create( "Default" )
                                        .AddReferences( MetadataReference.CreateFromFile( CommandParams.FilePaths.ExigoDll ) )
                                        .AddSyntaxTrees( tree );

        SemanticModel model = _comp.GetSemanticModel( tree );
        var results = new List<RecordDeclarationSyntax>();

        foreach ( var cls in classes )
        {
            if ( cls.Ignore() )
                continue;

            results.Add( cls.ToRecordSyntax( model ) );
        }

        return results;
    }
    static IEnumerable<EnumDeclarationSyntax> GetEnumDeclarations( CompilationUnitSyntax fileRoot )
        => fileRoot.DescendantNodes().OfType<EnumDeclarationSyntax>();
    static IEnumerable<InterfaceDeclarationSyntax> GetInterfaceDeclarations( CompilationUnitSyntax fileRoot )
        => fileRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>();




    static PropertyDeclarationSyntax UpdatePropertySyntax( this PropertyDeclarationSyntax propSyntax , string className , SemanticModel model )
    {
        var idTxt = Replacements
                        .FirstOrDefault( r => r.ClassName.Equals(className) && r.OldValue.Equals( propSyntax.Identifier.ValueText ) )
                        .NewValue;

        var isApiUpdate = !string.IsNullOrEmpty(idTxt);
        var isArrayUpdate = isApiUpdate && idTxt.Equals("OrderTys");

        var propName = isApiUpdate.Equals(false) ?
                            propSyntax.Identifier :
                            Identifier( idTxt );

        var propTypeName = propSyntax.Type.GetText().ToString();
        
        if( isApiUpdate )
            propTypeName = isArrayUpdate ? "int[]" : "int";
        else
        {
            //Check for a fully qualified name
            var nameParts = propTypeName.Split( '.' );
            propTypeName = nameParts.Count().Equals( 1 ) ?
                            propTypeName : nameParts[ ^1 ];
        }

        propTypeName = propTypeName.Trim();
        if ( _nullableTypeNames.Any( n => n.Equals(propTypeName))  )
            propTypeName += "?";

        var prop =
                PropertyDeclaration( ParseTypeName( propTypeName ) , propName )
                    .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
                    .AddAccessorListAccessors(
                        AccessorDeclaration( SyntaxKind.GetAccessorDeclaration ).WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ,
                        AccessorDeclaration( SyntaxKind.InitAccessorDeclaration ).WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) ;

        return prop;
    }
    static RecordDeclarationSyntax ToRecordSyntax( this ClassDeclarationSyntax @class , SemanticModel model )
    {
        var _cls = @class.AddBaseListTypes( SimpleBaseType( ParseName( CommandParams.AtlCoreMetadata.JsonInterfaceIdentifier )) );
        var _rec = RecordDeclaration( Token( SyntaxKind.RecordKeyword ), _cls.Identifier)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                    .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
                    .WithBaseList( _cls.BaseList );

        

        var props = @class.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        if ( props is null )
            return _rec;

        foreach ( var p in props )
        {
            var symbol = model.GetDeclaredSymbol( p );
            if ( symbol is not null && symbol.IsEligibleProperty() )
                _rec = _rec.AddMembers( p.UpdatePropertySyntax( _cls.Identifier.ValueText , model ) );
        }

        if ( _cls.Identifier.ValueText.Equals( "OrderResponse" ) )
            _rec = _rec.AddMembers( PropertyDeclaration( ParseTypeName( "string" ) , Identifier( "OrderTyDescription" ) )
            .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
            .AddAccessorListAccessors(
                AccessorDeclaration( SyntaxKind.GetAccessorDeclaration ).WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ,
                AccessorDeclaration( SyntaxKind.InitAccessorDeclaration ).WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) ) ) );

        return _rec.WithDefaultConstructor();
    }
    static RecordDeclarationSyntax WithDefaultConstructor( this RecordDeclarationSyntax record )
    {
        var block = Block();
        var properties = record.DescendantNodes().OfType<PropertyDeclarationSyntax>();

        foreach ( var prop in properties )
        {
            var typeString = prop.Type.ToString();

            if ( typeString.Equals( "string" ) )
                block = block.AddStatements( ExpressionStatement( ParseExpression( $"{prop.Identifier.ValueText} = String.Empty" ) ) );

            if ( typeString.Contains( "[]" ) )
            {
                if( typeString.Contains("[][]"))
                    typeString = typeString.Replace("[][]","[0][]");
                else typeString = typeString.Replace( "[]" , "[0]" );

                block = block.AddStatements( ExpressionStatement( ParseExpression( $"{prop.Identifier.ValueText} = new { typeString } " ) ) );
            }

            if( typeString.Contains("List<"))
               block = block.AddStatements( ExpressionStatement( ParseExpression( $"{prop.Identifier.ValueText} = new() " ) ) );

        }

        var ctor = ConstructorDeclaration(record.Identifier.ValueText)
                    .AddModifiers( Token( SyntaxKind.PublicKeyword ))
                    .WithBody( block );

        var addBaseCall = record.BaseList?.Types.Any( t => t.Type.GetText().ToString().StartsWith("I")) ?? false;
        if ( addBaseCall )
            ctor = ctor.WithInitializer( ConstructorInitializer( Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseConstructorInitializer ) );

        return record.AddMembers( ctor );
    }
    static bool Ignore( this ClassDeclarationSyntax @class ) => Exclusions.Any( t => @class.Identifier.ValueText.Contains( t ) );
    static bool IsEligibleProperty( this ISymbol symbol )
    {
        return symbol is IPropertySymbol _sym
                && _sym.IsDefinition
                && !_sym.IsStatic
                && !_sym.IsReadOnly
                && !_sym.DeclaredAccessibility.Equals( Accessibility.Private );
    }

    #region File Operations

    static StringBuilder InitializeFileBuilder()
    {
        var sb = new StringBuilder();
        sb.AppendLine( $"// Generated from source code found in Exigo.Api.Client. " );
        sb.AppendLine( "// Manual modification of files not recommended, could result in errors when using Http Services." );

        sb.AppendLine();
        sb.AppendLine();

        sb.AppendLine( "using System.Data;" );
        sb.AppendLine( $"namespace {CommandParams.AtlCoreMetadata.ExigoApiModelsNamespace};" );

        return sb;
    }
    static void WriteRecordFiles( DirectoryInfo outDirectory )
    {
        foreach ( var rec in _updatedRecords )
        {
            var srcFile = InitializeFileBuilder();
            var _rec = (RecordDeclarationSyntax) Formatter.Format( rec, _ws );
            srcFile.Append( _rec.ToString() );

            var filePath = Path.Combine( outDirectory.FullName, _rec.Identifier.Text + ".cs");
            File.WriteAllText( filePath , srcFile.ToString() );
        }
    }
    static void WriteEnumFile( DirectoryInfo outDirectory )
    {
        var path = Path.Combine( outDirectory.FullName, "_Enums.cs" );
        File.WriteAllText( path , _enumFile.ToString() );
    }
    static void WriteInterfaceFile( DirectoryInfo outDirectory )
    {
        var path = Path.Combine( outDirectory.FullName, "_Interfaces.cs" );
        File.WriteAllText( path , _interfaceFile.ToString() );
    }
    static void AppendEnumToFile( EnumDeclarationSyntax enumDeclaration )
    {
        var formatted = (EnumDeclarationSyntax) Formatter.Format( enumDeclaration, _ws );

        _enumFile.Append( formatted.ToString() );
        _enumFile.AppendLine();
    }
    static void AppendInterfaceToFile( InterfaceDeclarationSyntax interfaceDeclaration )
    {
        var formatted = (InterfaceDeclarationSyntax) Formatter.Format( interfaceDeclaration, _ws );

        _interfaceFile.Append( formatted.ToString() );
        _interfaceFile.AppendLine();
    }
    static string OutPath( bool writeToProject )
    => writeToProject ?
        Path.Combine( CommandParams.ProjectDirectoryPaths.Atl_ExigoIntegration , "Api" , "Models" ) :
        Path.Combine( CommandParams.TestDirectoryPaths.NamedClientEntities , $"ExigoGeneratorTest_{new DateTimeOffset( DateTime.Now ).ToUnixTimeSeconds().ToString()}" );

    #endregion

    // API Version 2022.10.24.5 Changes
    static readonly List<( string ClassName ,string OldValue, string NewValue )> Replacements = new List<(string ClassName ,string OldValue, string NewValue )>
    {
        ( "GetOrdersRequest",  "OrderTypes","OrderTys" ) ,
        ( "OrderResponse",  "OrderType","OrderTyID" ) ,
        ( "CreateCustomer",  "PayableType","PayableTy" ) ,
        ( "UpdateCustomer",  "TaxIDType","TaxIDTy" ) ,
        ( "UpdateCustomer",  "PayableType","PayableTy" ) ,
        ( "CreateExpectedPayment",  "PaymentType","PaymentTy" ) ,
        ( "CreatePayment",  "PaymentType","PaymentTy" ) ,
        ( "PaymentResponse",  "PaymentType","PaymentTypeId" ) ,
        ( "ExpectedPaymentResponse",  "PaymentType","PaymentTypeId" ) ,
        ( "CreatePointTransactionRequest",  "TransactionType","PointTransactionTy" )
    };
}
