


internal struct DirectoryLocations
{
    public const string DecompiledExigoDllFiles = @"C:\Users\ashto\source\repos\AtlConsultingIO.Generator\ExigoApi_Decompiled";
    public const string AtlIoSolution = @"C:\Users\ashto\source\repos\AtlConsultingIo";
    public const string Repos = @"C:\Users\ashto\source\repos";

    public const string JsonConfigDirectory = @"C:\Users\ashto\source\repos\AtlConsultingIo\AtlConsultingIo.DevOps\_config";

    public struct LocalOutputs
    {
        public const string TestDirectory = @"C:\Users\ashto\Desktop\_TestOutput";
        public const string LogDirectory = @"C:\Users\ashto\Desktop\_LogOutput";
        public const string BackupDirectory = @"C:\Users\ashto\Desktop\_BackupOutput";

        public const string GeneratedClientModelsTest = TestDirectory + @"\NamedClients";
        public const string GeneratedSqlEntitiesTest = TestDirectory + @"\DbEntities";
    }
    public struct Projects
    {
        public const string Integrations = Repos + @"\AtlConsultingIO.Integrations";
        public const string Infrastructure = AtlIoSolution + @"\AtlConsultingIo.Infrastructure";
        public const string ExigoMetadata = AtlIoSolution + @"\AtlConsultingIo.ExigoMetadata";
        public const string IntegratedEntities = AtlIoSolution + @"\AtlConsultingIo.IntegratedEntities";
        public const string NamedClients = AtlIoSolution + @"\AtlConsultingIo.NamedClients";
        public const string This = AtlIoSolution + @"\AtlConsultingIo.DevOps";

        public const string BuildOutputBase = @"C:\Users\ashto\source\repos\AtlConsultingIo\_BuildOutput";
    }
}
