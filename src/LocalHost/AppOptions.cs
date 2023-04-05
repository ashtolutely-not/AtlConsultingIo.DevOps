using Microsoft.Extensions.Configuration;


namespace AtlConsultingIo.DevOps.LocalHost;

public record AppOptions
{
    private IConfiguration _configuration;

    public AppOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool ContainsKey(string key) => _configuration[key] is not null;
    public T? TryGet<T>(string key) where T : class
    {
        if (!ContainsKey(key))
            return default;

        return _configuration.GetSection(key).Get<T>();
    }

    public string? Get( string key ) => _configuration[key];
}
