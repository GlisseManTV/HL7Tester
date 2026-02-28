using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace HL7TesterMac.Logging;

/// <summary>
/// Logger très simple qui écrit les logs dans des fichiers texte datés par jour
/// (un fichier par date) dans un dossier donné.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directoryPath;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly LogLevel _minLogLevel;

    public FileLoggerProvider(string directoryPath, LogLevel minLogLevel = LogLevel.Information)
    {
        _directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new FileLogger(_directoryPath, _lock, name, _minLogLevel));

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _directoryPath;
        private readonly object _lock;
        private readonly string _categoryName;
        private readonly LogLevel _minLogLevel;

        public FileLogger(string directoryPath, object syncRoot, string categoryName, LogLevel minLogLevel)
        {
            _directoryPath = directoryPath;
            _lock = syncRoot;
            _categoryName = categoryName;
            _minLogLevel = minLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {_categoryName}: {message}";
            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                // Un fichier par jour : yyyyMMdd.log
                var currentDate = DateTimeOffset.Now;
                var fileName = $"{currentDate:yyyyMMdd}.log";
                var filePath = Path.Combine(_directoryPath, fileName);

                Directory.CreateDirectory(_directoryPath);
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
