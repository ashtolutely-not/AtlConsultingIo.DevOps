
using System.Text.RegularExpressions;

internal static class TextUtils
{
    public const string WhitespaceChar = " ";
    public const string DoubleQuoteChar = "\"";
    public const string SingleQuoteChar = "'";

    public static string SurroundWithDoubleQuotes( this string value ) => string.Concat( DoubleQuoteChar , value , DoubleQuoteChar );
    public static string SurroundWithSingleQuotes( this string value ) => string.Concat( SingleQuoteChar , value , SingleQuoteChar );
    public static string SurroundWithWhitespace( this string value ) => string.Concat( WhitespaceChar , value , WhitespaceChar );

    public static string StartWithWhitespace( this string value ) => string.Concat( WhitespaceChar , value );
    public static string EndWithWhitespace( this string value ) => string.Concat( value , WhitespaceChar );

    public static string UniqueFileName( this string fileNameBase )
        => string.IsNullOrEmpty( fileNameBase ) ?
            DateTimeTag :
            string.Join( "_" , fileNameBase , DateTimeTag );

    public static string WithDateTagSuffix( this string value ) => string.Join("_", value , DateTimeTag );
    public static string DateTimeTag => new DateTimeOffset( DateTime.Now ).ToUnixTimeSeconds().ToString();
    public static string RemoveSpecialCharacters( this string value )
    {
        return Regex.Replace( value , "[^a-zA-Z0-9_.]+" , "" , RegexOptions.Compiled );
    }
}
