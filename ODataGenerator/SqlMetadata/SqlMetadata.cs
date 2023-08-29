

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AtlConsultingIo.DevOps;

public record SqlMetadata
{
    public string DatabaseName { get; init; } = String.Empty;
    public string SchemaName { get; init; } = String.Empty;
    public string TableName { get; init; } = String.Empty;
    public string Name { get; init; } = String.Empty;


}
