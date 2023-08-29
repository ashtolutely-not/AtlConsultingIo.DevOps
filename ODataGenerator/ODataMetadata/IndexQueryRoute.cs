namespace AtlConsultingIo.DevOps;

internal record IndexQueryRoute
{
    public string PluralizedTableName { get; init; } = String.Empty;
    public List<IndexRouteParam> RouteParams { get; init; } = new();
    public string ActionName => "Get" + PluralizedTableName;
}
