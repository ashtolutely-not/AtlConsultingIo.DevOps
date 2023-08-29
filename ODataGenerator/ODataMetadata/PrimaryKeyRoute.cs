namespace AtlConsultingIo.DevOps;

internal record PrimaryKeyRoute
{
    private static readonly SqlIndexMetadata _defaultIndex = new();
    public SqlIndexMetadata PrimaryKeyIndex { get; init; } 
    public PrimaryKeyRoute( SqlIndexMetadata index )
    {
        PrimaryKeyIndex = index;
    }

    private PrimaryKeyRoute( )
    {
        PrimaryKeyIndex = _defaultIndex;
    }

    public static readonly PrimaryKeyRoute Empty = new();
}