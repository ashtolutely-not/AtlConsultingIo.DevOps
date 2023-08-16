

using AtlConsultingIo.DevOps.LocalHost;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtlConsultingIo.DevOps;
public class AzureClient
{
    private readonly ILogger _logger;
    private readonly AppConfiguration _options;
    public AzureClient( ILogger logger , IOptions<AppConfiguration> options )
    {
        _logger = logger;
        _options = options.Value;
    }

    private ArmClient? _tlc { get; set; }
    private ArmClient? _dev { get; set; }

    private async Task Playground()
    {
        _dev ??= new ArmClient(GetDevCredential());

        var sub = await _dev.GetDefaultSubscriptionAsync();
        NullableResponse<ResourceGroupResource> result = await sub.GetResourceGroupAsync("");

        if( result.HasValue )
        {
            var resourceGroup = result.Value;

            
        }
    }

    public TokenCredential GetTlcCredential()
    {
        var id = _options.Get( CommandParams.SecretKeys.TlcAzureIdentity );

        return !string.IsNullOrEmpty( id ) ? new DefaultAzureCredential( new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = id ,
        } ) : new DefaultAzureCredential();
    }

    public TokenCredential GetDevCredential()
    {
        var id = _options.Get( CommandParams.SecretKeys.DevAzureIdentity );

        return !string.IsNullOrEmpty( id ) ? new DefaultAzureCredential( new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = id ,
        } ) : new DefaultAzureCredential();
    }
}
