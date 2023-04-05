namespace AtlConsultingIo.Generators;
internal partial record struct EFCoreCliCommand
{
    public const string Alias = "dotnet ef";
    public const string ExePath = @"C:\Users\ashto\.dotnet\tools\dotnet-ef.exe";

    public const string ContextNameArg = "--context";
    public const string TargetProjectArg =  "--project";
    public const string StartupProjectArg = "--startup-project";

    public const string VerboseFlag =  "--verbose";
    public const string NoBuildFlag = "--no-build";
}
