
using System.Reflection.Metadata.Ecma335;
using System.Xml;

namespace AtlConsultingIo.DevOps.PackageManager;

public record struct PackageVersion
{
    public string VersionType { get; init; }
    public int MajorVersion { get; init; }
    public int MinorVersion { get; init; }
    public int PatchVersion { get; init; }

    public string CommandArg => VersionType.Equals(PackageVersionType.Release) ? 
                                string.Join('.', MajorVersion, MinorVersion, PatchVersion) : 
                                string.Join('.', MajorVersion, MinorVersion, PatchVersion) + $"-{VersionType}";

    public PackageVersion WithNewPatchVersion => this with { PatchVersion = PatchVersion + 1 };
    public PackageVersion WithNewMinorVersion => this with 
    { 
        MinorVersion = MinorVersion + 1,
        PatchVersion = 0    
    }; 
    public PackageVersion WithNewMajorVersion => this with 
    { 
        MajorVersion = MajorVersion + 1 ,
        MinorVersion = 0,
        PatchVersion = 0
    };

    public static readonly PackageVersion None = new();


    public PackageVersion( string Prefix, string Suffix )
    {
        VersionType = Suffix;

        var parts = Prefix.Split('.');

        MajorVersion = int.TryParse( parts[0], out int _major) ? _major: 0;
        MinorVersion = parts.Length >= 2 && int.TryParse( parts[1], out int _minor) ? _minor : 0;
        PatchVersion = parts.Length >= 3 && int.TryParse ( parts[2], out int _patch) ? _patch : 0;  

    }

    public static PackageVersion ParseXmlVersion ( ProjectDirectory project )
    {
        if( project.GetProjectXml() is XmlNode node && node.ChildNodes is XmlNodeList nodes)
            foreach( XmlNode n in nodes )
                if( n.Name.Equals("PropertyGroup") && n.ChildNodes is XmlNodeList properties )
                {
                    string? prefix = null;
                    string? suffix = null;

                    foreach( XmlNode prop in properties )
                        if( prop.Name.Equals("VersionPrefix") ) prefix = prop.Value;
                        else if( prop.Name.Equals("VersionSuffix")) suffix = prop.Value;
                        else continue;

                    if( prefix is string _pre && suffix is string _suff )
                        return new( _pre, _suff );
                }    
                        

        return PackageVersion.None;
    }

}


public struct PackageVersionType
{
    public const string Alpha = nameof(Alpha);
    public const string Beta = nameof(Beta);
    public const string Preview = nameof(Preview);
    public const string Release = nameof(Release);
}

