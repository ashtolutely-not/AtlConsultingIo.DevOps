//using Microsoft.EntityFrameworkCore.Metadata;
//using Microsoft.EntityFrameworkCore;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Options;
//using Microsoft.CodeAnalysis.Text;
//using Microsoft.CodeAnalysis;
//using System.Collections.Immutable;

//namespace AtlConsultingIo.DevOps.src.Generators;
//internal static class ExigoControllerGenerator
//{

//    private static Workspace _ws = new AdhocWorkspace();
//    private static string _dir = "c:\\users\\ashto\\desktop\\ODataControllers";



//    //public static IEnumerable<(Type EntityType, PropertyInfo DbSetProperty)> DbSetMeta => _ctx == null ?
//    //                                                                                    null :
//    //                                                                                    _ctx.GetType().GetProperties()
//    //                                                                                    .Where(p => p.PropertyType.IsGenericType
//    //                                                                                                && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
//    //                                                                                                && p.PropertyType.GetGenericArguments()[0] != null)
//    //                                                                                    ?.Select(p => (p.PropertyType.GetGenericArguments()[0], p));

//    public static void Run( string EntitiesDirectoryPath , string ContextFilePath )
//    {
//        foreach ( var e in Entities )
//        {
//            if ( e.GetKeys() == null || e.GetKeys().Count() == 0 )
//                continue;

//            var classNameBase = DbSetMeta.FirstOrDefault(s => s.EntityType == e.ClrType).DbSetProperty.Name;
//            var cls = Class(classNameBase)
//                .AddMembers(ReadonlyProperties(e).ToArray())
//                .AddMembers(Constructor(classNameBase, e))
//                .AddMembers(QueryableMethod(e,classNameBase))
//                .AddMembers(SingleResultMethod(e));
//            cls = (ClassDeclarationSyntax) Formatter.Format( cls , _ws );

//            var ns = (NamespaceDeclarationSyntax)Formatter.Format(Namespace().AddMembers(cls), _ws);
//            File.WriteAllText( Path.Combine( _dir , $"{classNameBase}Controller.cs" ) , ns.ToFullString() );
//        }
//    }

//    public static NamespaceDeclarationSyntax Namespace() => NamespaceDeclaration( ParseName( "TotalLife.ODataService" ) )
//    .AddUsings( new UsingDirectiveSyntax[]
//    {
//        UsingDirective(ParseName("Microsoft.AspNetCore.OData.Routing.Controllers")),
//        UsingDirective(ParseName("Microsoft.EntityFrameworkCore")),
//        UsingDirective(ParseName("Microsoft.AspNetCore.Mvc")),
//        UsingDirective(ParseName("EntityFramework.TotalLifeReporting")),
//    } );

//    public static ClassDeclarationSyntax Class( string classNameBase ) =>
//            ClassDeclaration( $"{classNameBase}Controller" )
//            .WithBaseList( BaseList( SingletonSeparatedList<BaseTypeSyntax>( SyntaxFactory.SimpleBaseType( ParseName( "ODataController" ) ) ) ) )
//            .AddModifiers( Token( SyntaxKind.PublicKeyword ) );

//    public static List<PropertyDeclarationSyntax> ReadonlyProperties( IEntityType EntityType )
//    {
//        return new List<PropertyDeclarationSyntax>
//            {
//                PropertyDeclaration(ParseTypeName((typeof(TotalLifeReportingContext).Name)), "_ctx")
//                    .AddModifiers(Token(SyntaxKind.PrivateKeyword))
//                    .AddModifiers(Token(SyntaxKind.ReadOnlyKeyword))
//                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
//                PropertyDeclaration(GenericName(Identifier("DbSet"))
//                    .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(ParseTypeName(EntityType.ClrType.Name)))),"_tbl")
//                    .AddModifiers(Token(SyntaxKind.PrivateKeyword))
//                    .AddModifiers(Token(SyntaxKind.ReadOnlyKeyword))
//                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
//            };
//    }
//    public static ConstructorDeclarationSyntax Constructor( string classNameBase , IEntityType EntityType ) => (ConstructorDeclarationSyntax)
//        Formatter.Format(
//            ConstructorDeclaration( $"{classNameBase}Controller" )
//            .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
//            .AddParameterListParameters( new ParameterSyntax[]
//            {
//                    Parameter(attributeLists: default,
//                                            modifiers: default,
//                                            type: ParseTypeName("TotalLifeReportingContext"),
//                                            identifier: Identifier("dbContext"),
//                                            @default: null),
//                    Parameter(attributeLists: default,
//                                            modifiers: default,
//                                            type: ParseTypeName("IConfiguration"),
//                                            identifier: Identifier("configuration"),
//                                            @default: null)
//            } )
//            .WithBody( ConstructorBlock( EntityType ) ) , _ws );

//    private static BlockSyntax ConstructorBlock( IEntityType EntityType ) => (BlockSyntax)
//        Formatter.Format( Block( new StatementSyntax[]
//            {
//                ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,ParseExpression("_ctx "),ParseExpression("dbContext"))),
//                ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,ParseExpression("_tbl "),ParseExpression($"_ctx.Set<{EntityType.ClrType.Name}>()")))
//            } ) , _ws );


//    public static MethodDeclarationSyntax QueryableMethod( IEntityType EntityType , string baseClassName ) => (MethodDeclarationSyntax)
//        Formatter.Format( MethodDeclaration( ParseTypeName( $"ActionResult<IQueryable<{EntityType.ClrType.Name}>>" ) , $"Get{baseClassName}" )
//        .AddAttributeLists( EnableQueryAttribute )
//        .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
//        .AddBodyStatements( ParseStatement( "return _tbl;" ) ) , _ws );

//    public static MethodDeclarationSyntax SingleResultMethod( IEntityType EntityType ) => (MethodDeclarationSyntax)
//        Formatter.Format( MethodDeclaration( ParseTypeName( $"ActionResult<{EntityType.ClrType.Name}>" ) , $"Get" )
//        .AddAttributeLists( EnableQueryAttribute )
//        .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
//        .AddParameterListParameters( Parameters( EntityType ) )
//        .WithBody( SingleResultBody( EntityType ) ) , _ws );

//    private static BlockSyntax SingleResultBody( IEntityType EntityType )
//    {
//        var conditions = new List<string>();
//        foreach ( var k in EntityType.GetKeys() )
//            foreach ( var p in k.Properties )
//                conditions.Add( $"r.{p.Name} == key{p.Name}" );

//        return (BlockSyntax) Formatter.Format( Block( new StatementSyntax[]
//        {
//            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, ParseExpression("var result "), ParseExpression($"_tbl.FirstOrDefault(r => {string.Join(" && ", conditions)} )"))),
//            ReturnStatement(ParseExpression("result == default ? NotFound() : Ok(result)"))
//        } ) , _ws );
//    }
//    private static ParameterSyntax[] Parameters( IEntityType EntityType )
//    {
//        var list = new List<ParameterSyntax>();
//        foreach ( var k in EntityType.GetKeys() )
//            foreach ( var p in k.Properties )
//                list.Add( Parameter( attributeLists: default ,
//                                    modifiers: default ,
//                                    type: ParseTypeName( TypeKeyword( Type.GetTypeCode( p.ClrType ) ) ) ,
//                                    identifier: Identifier( $"key{p.Name}" ) ,
//                                    @default: null ) );

//        return list.ToArray();
//    }

//    private static string TypeKeyword( TypeCode code ) => code switch
//    {
//        TypeCode.Int64 => "int",
//        TypeCode.Int32 => "int",
//        TypeCode.Boolean => "bool",
//        TypeCode.String => "string",
//        TypeCode.Double => "double",
//        TypeCode.DateTime => "DateTime",
//        TypeCode.Decimal => "decimal",
//        TypeCode.Int16 => "int",
//        TypeCode.Byte => "byte",
//        _ => string.Empty
//    };
//    private static AttributeListSyntax EnableQueryAttribute => AttributeList( SingletonSeparatedList<AttributeSyntax>( Attribute( IdentifierName( "EnableQuery" ) ) ) );

//    private static ClassDeclarationSyntax? GetContextClassDeclaration( string ContextFilePath )
//    {
//        var file = new FileInfo( ContextFilePath );
//        if ( !file.Exists )
//            throw new ArgumentException( $"Invalid file path for DB Context file {ContextFilePath}" );

//        string fileTxt = File.ReadAllText( file.FullName );
//        SyntaxTree tree = CSharpSyntaxTree.ParseText( fileTxt );

//        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

//        var ctxClass = root
//                        .DescendantNodes()
//                        .FirstOrDefault( n => n is ClassDeclarationSyntax _cls
//                                                && _cls.BaseList is BaseListSyntax _base
//                                                && _base.Types.Select( t => t.ToString()).Contains("DbContext") );

//        return ctxClass is not null ? (ClassDeclarationSyntax) ctxClass : null;

//    }

//    private static IEnumerable<PropertyDeclarationSyntax> GetEntitySetProps( ClassDeclarationSyntax dbContextClass )
//        => dbContextClass
//            .ChildNodes()
//            .Where( n => n is PropertyDeclarationSyntax _prop && _prop.Type.ToString().Contains( "DbSet" ) )
//            ?.Cast<PropertyDeclarationSyntax>() ?? Enumerable.Empty<PropertyDeclarationSyntax>();

//    private static string GetEntityName( PropertyDeclarationSyntax DbSetProp )
//        => DbSetProp.Type.ToString().Replace( "DbSet<" , "" ).Replace( ">" , "" );

//    private static IEnumerable<FileInfo> GetEntitySourceFiles( string EntitiesDirectoryPath )
//    {
//        var dir = new DirectoryInfo( EntitiesDirectoryPath );
//        if ( !dir.Exists )
//            throw new ArgumentException( $"Invalid entities directory {EntitiesDirectoryPath}" );

//        return dir.GetFiles( "*.cs" , SearchOption.AllDirectories );
//    }

//    private static async Task<(Workspace Workspace, Compilation Compilation)> BuildEntitiesWorkItems( IEnumerable<FileInfo> EntityFiles )
//    {
//        ProjectId id = ProjectId.CreateNewId();
//        VersionStamp version = VersionStamp.Create();

//        AdhocWorkspace ws = new AdhocWorkspace();
//        string assemblyName = "DbEntities.TempAssembly";

//        Project project = ws.AddProject(
//            ProjectInfo.Create( id, version, assemblyName, assemblyName, LanguageNames.CSharp)
//        );

//        Compilation comp = CSharpCompilation.Create( assemblyName );
//        List<SyntaxTree> trees = new();
//        foreach ( var srcFile in EntityFiles )
//        {
//            var info = GetDocumentInfo( srcFile, id );
//            var doc = ws.AddDocument( info );

//            if ( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree && !trees.Contains( _tree ) )
//                trees.Add( _tree );
//        }

//        if ( trees.Any() )
//            comp = comp.AddSyntaxTrees( trees );

//        return (ws, comp);
//    }

//    static DocumentInfo GetDocumentInfo( FileInfo sourceFile , ProjectId projectId )
//    {
//        SourceText txt = SourceText.From( File.ReadAllText( sourceFile.FullName ) );
//        TextLoader loader = TextLoader.From( TextAndVersion.Create( txt, VersionStamp.Create(), sourceFile.FullName ));
//        DocumentInfo info = DocumentInfo
//                                .Create( DocumentId.CreateNewId( projectId ), sourceFile.Name, loader: loader )
//                                .WithFilePath( sourceFile.FullName);

//        return info;
//    }

//    static void DoWork( PropertyDeclarationSyntax EntitySetProp , CSharpCompilation EntityFilesCompilation , AdhocWorkspace Workspace )
//    {
//        var entityTypeName = GetEntityName( EntitySetProp );
//        var entityTypeSymbol = EntityFilesCompilation.GetTypeByMetadataName( entityTypeName );

//        if ( entityTypeSymbol is null ) return;

//        var controllerClass = ControllerClassDeclaration( EntitySetProp );



//    }

//    static void BuildQueryActionMethod( ClassDeclarationSyntax EntityClass , Compilation EntitiesCompilation , IndexAttributeColumns indexColumns )
//    {

//    }

//    static void BuildSingleEntityMethod( ClassDeclarationSyntax EntityClass , IEnumerable<(string KeyName, string KeyTypeName)> PrimaryKeys )
//    {

//    }
//    static IEnumerable<(string KeyName, string KeyType)> PrimaryKeyRouteAttributes( ClassDeclarationSyntax EntityTypeClass )
//    {
//        var props = EntityTypeClass.ChildNodes().OfType<PropertyDeclarationSyntax>();
//        var keyProps = new List<PropertyDeclarationSyntax>();

//        foreach ( var prop in props )
//        {
//            foreach ( var list in prop.AttributeLists )
//            {
//                var attrs = list.Attributes;
//                if ( list.Attributes.Any( a => a.Name.Equals( "Key" ) ) )
//                    keyProps.Add( prop );
//            }
//        }

//        return keyProps.Select( p => (p.Identifier.Text, p.Type.ToString()) );
//    }

//    static IEnumerable<IndexAttributeColumns> GetEntityIndexes( INamedTypeSymbol EntityTypeSymbol )
//    {
//        ImmutableArray<AttributeData> attributeData = EntityTypeSymbol.GetAttributes();
//        List<IndexAttributeColumns> indexes = new List<IndexAttributeColumns>();

//        foreach ( var attribute in attributeData )
//        {
//            if ( attribute.AttributeClass is null || !attribute.AttributeClass.Name.Equals( "IndexAttribute" ) ) continue;

//            var namedArgs = attribute.NamedArguments.Select( a => a.Value);
//            var args = attribute
//                        .ConstructorArguments
//                        .Where( x =>  !namedArgs.Any( a => a.Equals(x) ) ) ;

//            var names = new List<string>();
//            foreach ( var a in args )
//            {
//                if ( a.Value is string _val )
//                    names.Add( _val );

//                if ( a.Values.Any() )
//                    names.AddRange( a.Values.Where( v => v.Value is string ).Select( v => (string) v.Value! ) );
//            }
//            if ( names.Any() )
//                indexes.Add( new IndexAttributeColumns( names ) );
//        }

//        return indexes;
//    }

//    static ClassDeclarationSyntax ControllerClassDeclaration( PropertyDeclarationSyntax EntitySetProp )
//    {
//        return ClassDeclaration( Identifier( $"{EntitySetProp.Identifier.Text}Controller" ) )
//                .AddBaseListTypes( SimpleBaseType( ParseName( "ODataController" ) ) )
//                .AddAttributeLists( AttributeList( SingletonSeparatedList<AttributeSyntax>(
//                    Attribute(
//                            IdentifierName( "Route" ) ,
//                            AttributeArgumentList( SeparatedList<AttributeArgumentSyntax>( new AttributeArgumentSyntax[]
//                            {
//                                AttributeArgument( ParseExpression( Utils.SurroundWithDoubleQuotes($"odata/{EntitySetProp.Identifier.Text.ToLower()}") ) )
//                            } ) )
//                        ) ) )
//                );
//    }

//    readonly record struct IndexAttributeColumns( IEnumerable<string> Values );
//}
