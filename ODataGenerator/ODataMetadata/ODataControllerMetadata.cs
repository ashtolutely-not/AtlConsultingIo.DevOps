

using AtlConsultingIo.DevOps.ODataGenerator;
using AtlConsultingIo.Generators;

namespace AtlConsultingIo.DevOps;

internal record ODataControllerMetadata
{
    public int PageSize { get; init; }
    public EntitySourceMetadata TableEntityMetadata { get; init; }
    public DbContextCommandArgs ContextFileMeta { get; init; }
    public PrimaryKeyRoute PrimaryKeyRoute { get; init; } = PrimaryKeyRoute.Empty;
    public List<EntitySourceMetadata> RelatedEntities { get; init; } = new();
    public List<IndexQueryRoute> IndexQueryRoutes { get; init; } = new ( );
    public ODataControllerMetadata( EntitySourceMetadata tableMeta , DbContextCommandArgs fileContext )
    {
        TableEntityMetadata = tableMeta;
        ContextFileMeta = fileContext;
    }
}


