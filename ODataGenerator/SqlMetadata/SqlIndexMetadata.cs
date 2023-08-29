

namespace AtlConsultingIo.DevOps;

public record SqlIndexMetadata : SqlMetadata
{
    private static readonly List<SqlIndexColumn> _emptyColumns = new List<SqlIndexColumn>();
    public bool IsClustered{ get; init; } 
    public bool IsUnique { get; init; } 
    public bool IsPrimary { get; init; }
    public List<SqlIndexColumn> IndexColumns { get; init; } = _emptyColumns;

    public static string TableSelect( SqlTableMetadata tableMeta )
        => Select + $@" WHERE DatabaseName = '{ tableMeta.DatabaseName }' AND tab.Name = '{ tableMeta.Name }'";

    public const string Select = 
        $@"
            SELECT DISTINCT
                {nameof(DatabaseName)} = ts.TABLE_CATALOG
                ,{nameof(SchemaName)} = sch.name 
                ,{nameof(TableName)} = tab.name 
                ,{nameof(Name)} = ind.name 
                ,{nameof(IsClustered)} = IIF(ind.type_desc = 'CLUSTERED', 1, 0) 
                ,{nameof(IsUnique)} = ind.is_unique 
                ,{nameof(IsPrimary)} = ind.is_primary_key 	
                ,{nameof(SqlIndexColumn.ColumnName)} = col.name 
                ,{nameof(SqlIndexColumn.ColumnPosition)} = ic.key_ordinal 
	            ,{nameof(SqlIndexColumn.ColumnDataType)} = TYPE_NAME(col.system_type_id)
            FROM 
                sys.indexes ind
            INNER JOIN 
                sys.index_columns ic 
	            ON ind.object_id = ic.object_id AND ind.index_id = ic.index_id
            INNER JOIN 
                sys.tables tab 
	            ON ind.object_id = tab.object_id
            INNER JOIN 
                sys.schemas sch 
	            ON tab.schema_id = sch.schema_id
            INNER JOIN 
                sys.columns col 
	            ON ic.object_id = col.object_id 
	            AND ic.column_id = col.column_id
            INNER JOIN
	            INFORMATION_SCHEMA.TABLES ts
	            ON tab.name = ts.TABLE_NAME
        ";
}


public record SqlIndexColumn
{
    public string ColumnName { get; init; } = String.Empty;
    public int ColumnPosition { get; init; }
    public string ColumnDataType { get; init; } = String.Empty;
}
