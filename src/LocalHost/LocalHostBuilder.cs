using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace AtlConsultingIo.DevOps.LocalHost;
public static class LocalHostBuilder
{
    public static IHost GetLocalHost()
    {
        IHost localhost = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((ctx, bldr) =>
        {
            bldr.AddUserSecrets<LocalFileLogger>()
            .AddJsonFile("appsettings.json", false)
            .Build();
        })
        .ConfigureLogging((ctx, bldr) =>
        {
            bldr.ClearProviders();
            bldr.AddDebug();

            bldr
            .Services
            .Configure<LogFileOptions>(ctx.Configuration.GetSection(LogFileOptions.ConfigurationKey));

            bldr
            .Services
            .TryAddSingleton<ILoggerProvider, LocalFileLoggerProvider>();
        })
        .ConfigureServices((ctx, services) =>
        {
            var appOptions = new AppConfiguration(ctx.Configuration);
            services.AddLogging();
            services.AddOptions();
            services.ConfigureOptions(appOptions);

        })
        .Build();

        return localhost;
    }
}
