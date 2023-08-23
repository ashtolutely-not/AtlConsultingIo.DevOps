using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace AtlConsultingIo.DevOps.LocalHost;



[ProviderAlias("LocalFileLogger")]
public class LocalFileLoggerProvider : ILoggerProvider
{
    public readonly LogFileOptions Options;
    public LocalFileLoggerProvider(IOptions<LogFileOptions> options)
    {
        Options = options.Value;

        if (!Directory.Exists(Options.LogDirectoryPath))
            Directory.CreateDirectory(Options.LogDirectoryPath);
    }

    public ILogger CreateLogger(string categoryName)
        => new LocalFileLogger(this);

    public void Dispose() { }
}

public class LocalFileLogger : ILogger
{
    protected readonly LocalFileLoggerProvider _provider;
    private readonly LogFileOptions _options;

    public LocalFileLogger(LocalFileLoggerProvider textFileLogger)
    {
        _provider = textFileLogger;
        _options = _provider.Options;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var trace = exception is null ? string.Empty : exception.StackTrace;
        var message = formatter(state, exception) ?? string.Empty;

        var logRecord =
            string.Format(
                "[{0}] [{1}] {2} {3}",
                FormattedLogDate(),
                logLevel.ToString(),
                message,
                trace
            );

        var filePath = GetFilePath();
        if (!File.Exists(filePath))
            File.Create(filePath);

        using (var streamWriter = new StreamWriter(filePath, true))
        {
            streamWriter.WriteLine(Separator);
            streamWriter.WriteLine(logRecord);
            streamWriter.WriteLine(Separator);
        }
    }

    private string GetFileName() => string.Format("{0}_{1}.txt", _options.LogFileNameBase, DateTime.Now.ToShortDateString().RemoveSpecialCharacters());
    private string GetFilePath() => Path.Combine(_options.LogDirectoryPath, GetFileName());
    private string FormattedLogDate() => DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss+00:00");

    private static string? _separator;

    private static string Separator
    {
        get
        {
            if (string.IsNullOrEmpty(_separator))
            {
                _separator = string.Empty;
                int charCount = 100;

                while (charCount > 0)
                {
                    _separator += "-";
                    charCount--;
                }
            }

            return _separator;
        }
    }

}
