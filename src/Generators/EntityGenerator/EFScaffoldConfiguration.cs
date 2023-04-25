namespace AtlConsultingIo.Generators;
internal record struct EFScaffoldConfiguration( string ConnectionString, 
    string ContextName, 
    string ProjectDirectory,
    string ContextOutDirectory, 
    string ContextNamespace, 
    string EntitiesOutDirectory, 
    string EntitiesNamespace, 
    string[] ExcludedTables,
    Dictionary<string,string> EntityNameAdjustments,
    string Schema = "dbo",
    bool IncludeTableViews = false,
    bool UseExactNames = true, 
    bool UsePluralizer = true);


