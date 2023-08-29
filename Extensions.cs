
using System.Text.RegularExpressions;

using Pluralize.NET.Core;

internal static class Extensions
{
    public const string WhitespaceChar = " ";
    public const string DoubleQuoteChar = "\"";
    public const string SingleQuoteChar = "'";
    private static readonly Pluralizer pluralizer = new();

    public static bool IsNullOrWhitespace( this string? value ) 
        => value is null || value.Length == 0;
    public static string ToPascalCase( this string txt )
        => txt.Substring ( 0 , 1 ).ToUpper ( ) + txt.Substring ( 1 );

    public static string ToCamelCase( this string txt )
        => txt.Insert ( 0 , txt[ 0 ].ToString ( ).ToLower ( ) ).Remove ( 1 , 1 );
    public static string Pluralize( this string value )
    => pluralizer.Pluralize ( value );

    public static string Singularize( this string value )
        => pluralizer.Singularize ( value );
    public static string SurroundWithDoubleQuotes( this string value ) => string.Concat( DoubleQuoteChar , value , DoubleQuoteChar );
    public static string SurroundWithSingleQuotes( this string value ) => string.Concat( SingleQuoteChar , value , SingleQuoteChar );
    public static string SurroundWithWhitespace( this string value ) => string.Concat( WhitespaceChar , value , WhitespaceChar );
    public static bool CaseInsensitiveEquals( this string? value , string? other )
        => value is not null && other is not null && other.Equals( value , StringComparison.OrdinalIgnoreCase);
    public static string StartWithWhitespace( this string value ) => string.Concat( WhitespaceChar , value );
    public static string EndWithWhitespace( this string value ) => string.Concat( value , WhitespaceChar );

    public static string UniqueFileName( this string fileNameBase )
        => string.IsNullOrEmpty( fileNameBase ) ?
            DateTimeTag :
            string.Join( "_" , fileNameBase , DateTimeTag );
    public static string WithDateTagSuffix( this string value ) => string.Join( "_" , value , DateTimeTag );
    public static string DateTimeTag => new DateTimeOffset( DateTime.Now ).ToUnixTimeSeconds().ToString();
    public static string RemoveSpecialCharacters( this string value )
    {
        return Regex.Replace( value , "[^a-zA-Z0-9_.]+" , "" , RegexOptions.Compiled );
    }
    public static async Task<INamespaceSymbol?> NamespaceSymbol( this Document doc , Compilation compilation )
    {
        if ( await doc.GetSyntaxTreeAsync() is SyntaxTree _tree )
            if ( await doc.GetSyntaxRootAsync() is SyntaxNode _root )
                if ( _root.DescendantNodes().FirstOrDefault( n => n is FileScopedNamespaceDeclarationSyntax ) is FileScopedNamespaceDeclarationSyntax _namespace )
                    if ( compilation.GetSemanticModel( _tree ).GetDeclaredSymbol( _namespace ) is INamespaceSymbol _symbol )
                        return _symbol;

        return null;
    }
    public static IEnumerable<FileInfo> CSharpFiles( this DirectoryInfo directory )
        => directory.GetFiles( "*.cs" , SearchOption.TopDirectoryOnly );
    public static bool ContainsSourceFiles( this DirectoryInfo directory )
        => directory.GetFiles( "*.cs" , SearchOption.TopDirectoryOnly ).Any();
    public static async Task<IEnumerable<string>> UsingNamespaces( this Document doc )
    {
        if ( await doc.GetSyntaxRootAsync() is not SyntaxNode _root || await doc.GetSyntaxTreeAsync() is not SyntaxTree _tree )
            return Enumerable.Empty<string>();

        var usings =  _root.DescendantNodes().OfType<UsingDirectiveSyntax>();
        if ( !usings.Any() )
            return Enumerable.Empty<string>();

        var list = new List<string>();
        foreach ( var u in usings )
        {
            var name = u.DescendantNodes().OfType<NameSyntax>().FirstOrDefault();
            if ( name is not null )
                list.Add( name.ToString() );
        }

        return list;
    }
    private static readonly List<FileInfo> _emptyFileList = new(0);
    private static readonly List<DirectoryInfo> _emptyDirectoryList = new(0);
    public static List<FileInfo> SourceFilesInThisDirectory( this DirectoryInfo directory )
        => directory.GetFiles("*.cs", SearchOption.TopDirectoryOnly)?.ToList() ?? _emptyFileList;
}
