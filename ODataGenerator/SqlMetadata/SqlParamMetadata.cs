

namespace AtlConsultingIo.DevOps;

public record SqlParamMetadata : SqlMetadata
{
    public string ProcedureName { get; init; } = String.Empty;
    public int Position { get; init; }
    public SqlParamMode Mode { get; init; }
    public bool IsResult { get; init; }
    public string DataType { get; init; } = String.Empty;

    public static readonly string SelectStatement =
        $@"SELECT DISTINCT
            {nameof(DatabaseName)} = SPECIFIC_CATALOG,
            {nameof(TableName)} = SPECIFIC_TABLE,
            {nameof(ProcedureName)} = SPECIFIC_NAME,
            {nameof(Name)} = PARAMETER_NAME,
            {nameof(Position)} = ORDINAL_POSITION,
            {nameof(Mode)} = PARAMETER_MODE,
            {nameof(IsResult)} = IS_RESULT,
            {nameof(DataType)} = DATA_TYPE
           FROM INFORMATION_SCHEMA.PARAMETERS";
}
