namespace AtlConsultingIo.DevOps;

public record SqlTableMetadata
{
    public string DatabaseName { get; init; } = String.Empty;
    public string SchemaName { get; init; } = String.Empty;
    public string Name { get; init; } = String.Empty;
    public string QualifiedTableName => string.Join ( '.' , SchemaName , Name );

    public List<SqlColumnMetadata>? ColumnMetadata { get; init; }
}