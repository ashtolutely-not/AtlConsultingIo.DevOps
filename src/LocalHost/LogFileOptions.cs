using System;


namespace AtlConsultingIo.DevOps.LocalHost;
public record LogFileOptions
{
    public virtual string LogDirectoryPath { get; set; } = string.Empty;
    public virtual string LogFileNameBase { get; set; } = string.Empty;

    public LogFileOptions()
    {

    }

    public LogFileOptions( string path, string name )
    {
        LogDirectoryPath = path;
        LogFileNameBase= name;
    }

    public const string ConfigurationKey = nameof(LogFileOptions);

    public static readonly LogFileOptions None = new();

    public static readonly LogFileOptions Default = new( DirectoryLocations.LocalOutputs.LogDirectory, "DevOps_Log");
}
