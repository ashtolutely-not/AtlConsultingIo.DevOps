namespace AtlConsultingIo.DevOps;

public record SqlRoutineMetadata : SqlMetadata
{
    public string SqlText { get; init; } = String.Empty;
    public SqlRoutineType RoutineType { get; init; }

    public static readonly string SelectStatement =
        $@"SELECT DISTINCT
            {nameof(DatabaseName)} = SPECIFIC_CATALOG,
            {nameof(TableName)} = SPECIFIC_TABLE,
            {nameof(Name)} = ROUTINE_NAME,
            {nameof(SqlText)} = ROUTINE_DEFINITION
           FROM INFORMATION_SCHEMA.ROUTINES";
}
