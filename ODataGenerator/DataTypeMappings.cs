namespace AtlConsultingIo.DevOps.ODataGenerator;

internal struct DataTypeMappings
{
    public static readonly Dictionary<string,string> Values = new()
    {
        ["bigint"] = "long",
        ["binary"] = ByteArray,
        ["bit"] = BoolKeyword,
        ["char"] = "char[]",
        ["date"] = nameof(DateTime),
        ["datetime"] = nameof(DateTime),
        ["datetime2"] = nameof(DateTime),
        ["datetimeoffset"] = nameof(DateTimeOffset),
        ["decimal"] = DecimalKeyword,
        ["float"] = "double",
        ["hierarchyid"] = ObjectKeyword,
        ["image"] = ByteArray,
        ["int"] = IntKeyword,
        ["money"] = DecimalKeyword,
        ["nchar"] = StringKeyword,
        ["ntext"] = StringKeyword,
        ["numeric"] = DecimalKeyword,
        ["nvarchar"] = StringKeyword,
        ["real"] = nameof(Single),
        ["rowversion"] = ByteArray,
        ["smalldatetime"] = nameof(DateTime),
        ["smallint"] = IntKeyword,
        ["smallmoney"] = DecimalKeyword,
        ["sql_variant"] = ObjectKeyword,
        ["text"] = StringKeyword,
        ["time"] = nameof(TimeSpan),
        ["timestamp"] = ByteArray,
        ["tinyint"] = ByteKeyword,
        ["uniqueidentifier"] = nameof(Guid),
        ["varbinary"] = ByteArray,
        ["varchar"] = StringKeyword,
        ["xml"] = StringKeyword
    };


    const string StringKeyword = "string";
    const string ByteArray = "byte[]";
    const string IntKeyword = "int";
    const string BoolKeyword = "bool";
    const string DecimalKeyword = "decimal";
    const string ObjectKeyword = "object";
    const string ByteKeyword = "byte";
}