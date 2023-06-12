using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text;

namespace AtlConsultingIo.Generators;

internal static class DialCodeStructGenerator
{
    static readonly AdhocWorkspace workspace = new();
    public static void BuildDialCodesStruct( FileInfo jsonResourcePath , DirectoryInfo outputPath )
    {
        if ( !jsonResourcePath.Exists )
            throw new FileNotFoundException( $"The file {jsonResourcePath.FullName} does not exist." );

        var json = File.ReadAllText( jsonResourcePath.FullName );
        JArray? jarray = JToken.Parse( json ) as JArray;

        if ( jarray is null )
            throw new InvalidOperationException( "The json file is empty or does not contain any dial codes." );

        var dialCodes = new List<CountryDialCode>();
        foreach ( var itm in jarray )
            if ( ( itm as JObject ) is JObject _jobject && _jobject.ToObject<CountryDialCode>() is CountryDialCode code )
                dialCodes.Add( code );

        var @struct = DialCodesStruct( dialCodes );
        var ns = FileScopedNamespaceDeclaration(ParseName("AtlConsultingIo.Core"))
                .AddMembers( @struct );

        var formatted = Formatter.Format( ns, workspace );

        var codeFile = formatted.ToString();
        var @out = Path.Combine( outputPath.FullName, "CountryDialCodes.cs" );

        File.WriteAllText( @out , codeFile );
    }

    static StructDeclarationSyntax DialCodesStruct( List<CountryDialCode> dialCodes )
        => (StructDeclarationSyntax) Formatter.Format(
                StructDeclaration( "CountryDialCodes" )
                    .AddModifiers( Token( SyntaxKind.PublicKeyword ) )
                    .AddMembers( ValuesProperty( dialCodes ) ) ,
                workspace
            );

    static PropertyDeclarationSyntax ValuesProperty( List<CountryDialCode> values )
    {
        PropertyDeclarationSyntax prop
            = PropertyDeclaration( ParseTypeName("CountryDialCode[]"), "Values" )
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword))
                .WithInitializer( EqualsValueClause( ParseExpression( ArrayInitializerExpression(values)) ));

        return (PropertyDeclarationSyntax) Formatter.Format( prop , workspace );

    }

    static string ArrayInitializerExpression( List<CountryDialCode> dialCodes )
    {
        var sb = new StringBuilder(" new CountryDialCode[] ");
        sb.AppendLine( "{" );
        int lastIndex = dialCodes.Count - 1;
        for ( int i = 0; i < dialCodes.Count; i++ )
        {
            var code = dialCodes[i];
            var exp = InitializerExpression( code );
            if ( i == lastIndex ) sb.AppendLine( exp );
            else sb.AppendLine( exp + "," );
        }

        sb.Append( "};" );
        return sb.ToString();
    }
    static string InitializerExpression( CountryDialCode dialCode )
        => $"new CountryDialCode(\"{dialCode.CountryName}\", \"{dialCode.CountryCode}\", \"{dialCode.DialCode}\")";

}


public record struct CountryDialCode
{
    [JsonProperty( "name" )]
    public string CountryName { get; init; }

    [JsonProperty( "dial_code" )]
    public string DialCode { get; init; }

    [JsonProperty( "code" )]
    public string CountryCode { get; init; }
}