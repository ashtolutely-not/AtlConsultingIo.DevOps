namespace AtlConsultingIo.DevOps;

public record ForeignKeyMetadata : SqlMetadata
{
    public string ColumnName { get; init; } = String.Empty;  
    public string RelatedTableName { get; init; } = String.Empty;
    public string RelatedColumnName { get; init; } = String.Empty;
    public string RelatedSchemaName { get; init; } = String.Empty;
    public static string TableSelect( SqlMetadata tableMeta )
        => Select + $@" WHERE ts.TABLE_CATALOG = '{tableMeta.DatabaseName}' AND tab.name = '{tableMeta.TableName}'";

    public static readonly string Select = 
        $@"
            SELECT DISTINCT
                {nameof(DatabaseName)}= ts.TABLE_CATALOG,
                {nameof(SchemaName)} = sch.name,
                {nameof(TableName)} = tab.name,
                {nameof(Name)} = fk.name,
                {nameof(ColumnName)} = COL_NAME(fk.parent_object_id, fkc.parent_column_id),
                {nameof(RelatedSchemaName)} = sch_rt.name
                {nameof(RelatedTableName)} = rt.name,
                {nameof(RelatedColumnName)} = COL_NAME(fk.referenced_object_id, fkc.referenced_column_id)
            FROM 
                sys.foreign_keys AS fk
            INNER JOIN 
                sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN 
                sys.tables AS tab ON fk.parent_object_id = tab.object_id
            INNER JOIN 
                sys.schemas AS sch ON tab.schema_id = sch.schema_id
            INNER JOIN 
                sys.tables AS rt ON fk.referenced_object_id = rt.object_id
            INNER JOIN 
                sys.schemas AS sch_rt ON rt.schema_id = sch_rt.schema_id
            INNER JOIN
	            INFORMATION_SCHEMA.TABLES ts
	            ON tab.name = ts.TABLE_NAME
        ";
}