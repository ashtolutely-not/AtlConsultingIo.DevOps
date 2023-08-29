namespace AtlConsultingIo.DevOps;

public record SqlColumnMetadata : SqlMetadata
{
    public int Position { get; init;  } 
    public string DataType { get; init; } = String.Empty;
    public bool HasDefault { get; init; }
    public bool IsNullable { get; init; }
    public int? MaxLength { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsForeignKey { get; init; }

    public static string TableSelect( SqlTableMetadata tableMeta )
        => Select + @$" WHERE c.TABLE_CATALOG = '{ tableMeta.DatabaseName }' AND c.TABLE_Name = '{ tableMeta.Name }'";

    public static readonly string Select = 
        $@"
            SELECT DISTINCT
                 {nameof(DatabaseName)} = c.TABLE_CATALOG
                ,{nameof(SchemaName)} = c.TABLE_SCHEMA
                ,{nameof(TableName)} = c.TABLE_NAME
                ,{nameof(Name)} = c.COLUMN_NAME
                ,{nameof(Position)} = c.ORDINAL_POSITION
                ,{nameof(HasDefault)} = IIF(LEN(c.COLUMN_DEFAULT) < 0, 1, 0)
                ,{nameof(IsNullable)} = c.IS_NULLABLE
                ,{nameof(DataType)} = c.DATA_TYPE
                ,{nameof(MaxLength)} = c.CHARACTER_MAXIMUM_LENGTH
                ,{nameof(IsPrimaryKey)} = IIF(pk.COLUMN_NAME IS NOT NULL, 1, 0)
                ,{nameof(IsForeignKey)} = IIF(fk.COLUMN_NAME IS NOT NULL, 1, 0)
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT 
		            cu.TABLE_CATALOG
		            , cu.TABLE_SCHEMA
		            , cu.TABLE_NAME
		            , cu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
                INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
	            ON cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            )  pk 
            ON 
	            c.TABLE_CATALOG = pk.TABLE_CATALOG 
	            AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
	            AND c.TABLE_NAME = pk.TABLE_NAME 
	            AND c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT 
	            cu.TABLE_CATALOG
	            , cu.TABLE_SCHEMA
	            , cu.TABLE_NAME
	            , cu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
                INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
	            ON cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
            ) fk 
            ON 
	            c.TABLE_CATALOG = fk.TABLE_CATALOG 
	            AND c.TABLE_SCHEMA = fk.TABLE_SCHEMA 
	            AND c.TABLE_NAME = fk.TABLE_NAME 
	            AND c.COLUMN_NAME = fk.COLUMN_NAME
        ";
}