namespace AtlConsultingIo.DevOps.ODataGenerator;
public record struct ODataGeneratorSettings( 
    string ConnectionString, 
    string ODataProjectPath,
    string? EntityFrameworkProjectPath,
    string DbContextName, 
    string? DbContextDirectoryName, 
    string DbContextNamespace, 
    string DbEntitiesDirectoryName, 
    string DbEntitiesNamespace, 
    List<string> TableFilters,
    string? SchemaConstraint ,
    int MaxPageSize
    );
