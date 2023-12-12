using System.Globalization;
using Colors.Net;
using Microsoft.Extensions.Logging;
using static Colors.Net.StringStaticMethods;

namespace BackupCLI.Helpers;

public class CustomLogger(string path, bool quiet) : ILogger
{
    private readonly StreamWriter logFile = new(path, append: true) { AutoFlush = true };

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> _)
    {
        string time = DateTime.Now.ToString(CultureInfo.InvariantCulture);

        if (!quiet)
        {
            var timeString = DarkGray($"[{time}]");
            var levelString = GetColor(logLevel)($"[{logLevel}]");

            #if DEBUG
                string? message = exception is null ? state?.ToString() : exception.ToString();
            #else
                string? message = state?.ToString();
            #endif

            ColoredConsole.WriteLine($"{timeString} {levelString} {message}");
        }

        logFile.WriteLine($"[{time}] [{logLevel}] {exception}");
    }

    public void Dispose() => logFile.Dispose();

    public void Error(Exception e) => this.LogError(0, e, e.Message);

    public void Info(string message) => this.LogInformation(message);

    private static Func<string, RichString> GetColor(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Information => Cyan,
        LogLevel.Warning => Yellow,
        LogLevel.Error => Red,
        LogLevel.Critical => DarkRed,
        _ => DarkGray
    };
}
